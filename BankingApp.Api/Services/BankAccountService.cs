using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BankingApp.Api.Constants;
using BankingApp.Api.Data;
using BankingApp.Api.DTOs;
using BankingApp.Api.Entities;
using BankingApp.Api.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace BankingApp.Api.Services;

public sealed class BankAccountService : IBankAccountService
{
    private readonly BankingDbContext _dbContext;

    public BankAccountService(BankingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<BankAccount?> CreateAsync(
        Guid userId,
        string iban,
        CancellationToken cancellationToken
    )
    {
        User? user = await _dbContext.Users
            .FirstOrDefaultAsync(
                candidate => candidate.Id == userId,
                cancellationToken
            );

        if (user is null)
        {
            return null;
        }

        BankAccount account = new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Iban = iban,
            Balance = 0,
            User = user
        };

        _dbContext.BankAccounts.Add(account);

        await _dbContext.SaveChangesAsync(
            cancellationToken
        );

        return account;
    }

    public async Task<BankAccount?> GetByIdAsync(
    Guid id,
    CancellationToken cancellationToken
)
    {
        return await _dbContext.BankAccounts
            .AsNoTracking()
            .Include(account => account.User)
            .FirstOrDefaultAsync(
                account => account.Id == id,
                cancellationToken
            );
    }

    public async Task<TransferResult> TransferAsync(
       Guid fromAccountId,
       Guid toAccountId,
       decimal amount,
       string idempotencyKey,
       CancellationToken cancellationToken
   )
    {
        if (amount <= 0)
        {
            throw new ArgumentException(
                "Amount must be greater than zero."
            );
        }

        if (fromAccountId == toAccountId)
        {
            throw new ArgumentException(
                "Source and destination accounts must be different."
            );
        }

        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            throw new ArgumentException(
                "Idempotency key is required."
            );
        }

        return await ExecuteTransferWithRetryAsync(
            fromAccountId,
            toAccountId,
            amount,
            idempotencyKey,
            requestHash: CreateTransferRequestHash(
                fromAccountId,
                toAccountId,
                amount
            ),
            cancellationToken
        );
    }

    private async Task<TransferResult> ExecuteTransferWithRetryAsync(
        Guid fromAccountId,
        Guid toAccountId,
        decimal amount,
        string idempotencyKey,
        string requestHash,
        CancellationToken cancellationToken
    )
    {
        const int maxAttempts = 3;

        string scope = CreateIdempotencyScope(fromAccountId);

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                return await TransferInternalAsync(
                    fromAccountId,
                    toAccountId,
                    amount,
                    idempotencyKey,
                    scope,
                    requestHash,
                    cancellationToken
                );
            }
            catch (Exception exception)
                when (IsConcurrencyFailure(exception))
            {
                if (attempt == maxAttempts)
                {
                    throw;
                }

                // The failed attempt left the accounts and the idempotency
                // record in the change tracker with their post-transfer
                // values. Without clearing, identity resolution would hand the
                // retry the already-debited balance and debit it a second time.
                _dbContext.ChangeTracker.Clear();

                await Task.Delay(
                    TimeSpan.FromMilliseconds(100 * attempt),
                    cancellationToken
                );
            }
            catch (DbUpdateException exception)
                when (
                    exception.InnerException is PostgresException postgresException &&
                    postgresException.SqlState == "23505" &&
            postgresException.ConstraintName ==
                IdempotencyOperations.UniqueIndexName
                )
            {
                _dbContext.ChangeTracker.Clear();

                IdempotencyRecord? existing =
                    await _dbContext.IdempotencyRecords
                        .AsNoTracking()
                        .FirstOrDefaultAsync(
                            record =>
                                record.Scope == scope &&
                                record.Key == idempotencyKey &&
                                record.Operation == IdempotencyOperations.Transfer,
                            cancellationToken
                        );
                if (existing is null)
                {
                    throw;
                }

                if (existing.RequestHash != requestHash)
                {
                    throw new IdempotencyConflictException();
                }

                return new TransferResult(
                    Success: true,
                    IsReplay: true,
                    StatusCode: existing.ResponseStatusCode,
                    ResponseBody: existing.ResponseBody
                        ?? "Transfer completed successfully."
                );
            }
        }

        throw new InvalidOperationException(
            "Transfer retry loop finished unexpectedly."
        );
    }

    private async Task<TransferResult> TransferInternalAsync(
        Guid fromAccountId,
        Guid toAccountId,
        decimal amount,
        string idempotencyKey,
        string scope,
        string requestHash,
        CancellationToken cancellationToken
    )
    {
        await using IDbContextTransaction transaction =
            await _dbContext.Database.BeginTransactionAsync(
                cancellationToken
            );

        try
        {
            IdempotencyRecord? existing =
                await _dbContext.IdempotencyRecords
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        record =>
                            record.Scope == scope &&
                            record.Key == idempotencyKey &&
                            record.Operation == IdempotencyOperations.Transfer,
                        cancellationToken
                    );

            if (existing is not null)
            {
                if (existing.RequestHash != requestHash)
                {
                    throw new IdempotencyConflictException();
                }
                await transaction.CommitAsync(
                    cancellationToken
                );

                return new TransferResult(
            Success: true,
            IsReplay: true,
            StatusCode: existing.ResponseStatusCode,
            ResponseBody: existing.ResponseBody
            );
            }

            IdempotencyRecord idempotencyRecord = new()
            {
                Id = Guid.NewGuid(),
                Scope = scope,
                Key = idempotencyKey,
                Operation = IdempotencyOperations.Transfer,
                RequestHash = requestHash,
                ResponseStatusCode = 200,
                ResponseBody = "Transfer completed successfully.",
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.IdempotencyRecords.Add(
                idempotencyRecord
            );

            Guid firstId =
                fromAccountId.CompareTo(toAccountId) < 0
                    ? fromAccountId
                    : toAccountId;

            Guid secondId =
                fromAccountId.CompareTo(toAccountId) < 0
                    ? toAccountId
                    : fromAccountId;

            List<BankAccount> accounts =
                await _dbContext.BankAccounts
                    .FromSqlInterpolated(
                        $"""
                    SELECT *
                    FROM "BankAccounts"
                    WHERE "Id" IN ({firstId}, {secondId})
                    ORDER BY "Id"
                    FOR UPDATE
                    """
                    )
                    .ToListAsync(cancellationToken);

            BankAccount? fromAccount =
                accounts.FirstOrDefault(
                    account => account.Id == fromAccountId
                );

            BankAccount? toAccount =
                accounts.FirstOrDefault(
                    account => account.Id == toAccountId
                );

            if (fromAccount is null || toAccount is null)
            {
                await transaction.RollbackAsync(
                    cancellationToken
                );

                return new TransferResult(
                    Success: false,
                    IsReplay: false,
                    StatusCode: 404,
                    ResponseBody: "One or both accounts not found."
                );
            }

            if (fromAccount.Balance < amount)
            {
                throw new InsufficientFundsException();
            }

            fromAccount.Balance -= amount;
            toAccount.Balance += amount;

            Guid transferId = Guid.NewGuid();


            BankTransaction debitTransaction = new()
            {
                Id = Guid.NewGuid(),
                BankAccountId = fromAccount.Id,
                TransferId = transferId,
                Amount = -amount,
                CreatedAt = DateTime.UtcNow
            };

            BankTransaction creditTransaction = new()
            {
                Id = Guid.NewGuid(),
                BankAccountId = toAccount.Id,
                TransferId = transferId,
                Amount = amount,
                CreatedAt = DateTime.UtcNow
            };

            Transfer transfer = new()
            {
                Id = transferId,
                FromAccountId = fromAccount.Id,
                ToAccountId = toAccount.Id,
                Amount = amount,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Transfers.Add(transfer);
            _dbContext.BankTransactions.AddRange(
       debitTransaction,
       creditTransaction
   );

            await _dbContext.SaveChangesAsync(
                cancellationToken
            );

            await transaction.CommitAsync(
                cancellationToken
            );

            return new TransferResult(
                Success: true,
                IsReplay: false,
                StatusCode: 200,
                ResponseBody: "Transfer completed successfully."
            ); ;
        }
        catch
        {
            await transaction.RollbackAsync(
                cancellationToken
            );

            throw;
        }
    }
    private static string CreateTransferRequestHash(
        Guid fromAccountId,
        Guid toAccountId,
        decimal amount
    )
    {
        string payload =
                    $"{fromAccountId}:{toAccountId}:{NormalizeAmount(amount)}";

        byte[] bytes = Encoding.UTF8.GetBytes(payload);

        byte[] hash = SHA256.HashData(bytes);

        return Convert.ToHexString(hash);
    }

    private static string NormalizeAmount(decimal amount)
    {
        return amount.ToString(
            "0.############################",
            CultureInfo.InvariantCulture
        );
    }
    private static string CreateIdempotencyScope(Guid fromAccountId)
    {
        return fromAccountId.ToString();
    }

    private static bool IsConcurrencyFailure(Exception exception)
    {
        for (Exception? current = exception;
            current is not null;
            current = current.InnerException)
        {
            if (current is PostgresException postgresException &&
                (postgresException.SqlState == "40001" ||
                    postgresException.SqlState == "40P01"))
            {
                return true;
            }
        }

        return false;
    }
}
