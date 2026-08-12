using System;
using System.Threading;
using System.Threading.Tasks;
using BankingApp.Api.DTOs;
using BankingApp.Api.Entities;

namespace BankingApp.Api.Services;

public interface IBankAccountService
{
    Task<BankAccount?> CreateAsync(
        Guid userId,
        string iban,
        CancellationToken cancellationToken
    );

    Task<BankAccount?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken
    );

    Task<TransferResult> TransferAsync(
        Guid fromAccountId,
        Guid toAccountId,
        decimal amount,
        string idempotencyKey,
        CancellationToken cancellationToken
    );
}
