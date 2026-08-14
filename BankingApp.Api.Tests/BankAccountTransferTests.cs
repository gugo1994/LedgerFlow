using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using BankingApp.Api.Constants;
using BankingApp.Api.Data;
using BankingApp.Api.Entities;
using BankingApp.Api.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BankingApp.Api.Tests;

public sealed class BankAccountTransferTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public BankAccountTransferTests(
        CustomWebApplicationFactory factory
    )
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Transfer_WithSameIdempotencyKey_ShouldNotExecuteTwice()
    {
        Guid userId = Guid.NewGuid();
        Guid fromAccountId = Guid.NewGuid();
        Guid toAccountId = Guid.NewGuid();

        using IServiceScope scope =
            _factory.Services.CreateScope();

        BankingDbContext dbContext =
            scope.ServiceProvider
                .GetRequiredService<BankingDbContext>();

        User user = new()
        {
            Id = userId,
            Email = $"test-{Guid.NewGuid()}@example.com",
            FullName = "Test User",
            CreatedAt = DateTime.UtcNow
        };

        BankAccount fromAccount = new()
        {
            Id = fromAccountId,
            UserId = userId,
            Iban = $"TEST-FROM-{Guid.NewGuid()}",
            Balance = 1000m
        };

        BankAccount toAccount = new()
        {
            Id = toAccountId,
            UserId = userId,
            Iban = $"TEST-TO-{Guid.NewGuid()}",
            Balance = 0m
        };

        dbContext.Users.Add(user);
        dbContext.BankAccounts.AddRange(
            fromAccount,
            toAccount
        );

        await dbContext.SaveChangesAsync();
        string idempotencyKey =
                Guid.NewGuid().ToString();

        var requestBody = new
        {
            toAccountId,
            amount = 100m
        };

        HttpRequestMessage request1 = new(
            HttpMethod.Post,
            $"/api/bank-accounts/{fromAccountId}/transfer"
        );

        request1.Headers.Add(
            "Idempotency-Key",
            idempotencyKey
        );

        request1.Content =
            JsonContent.Create(requestBody);
        IConfiguration configuration =
_factory.Services.GetRequiredService<IConfiguration>();

        string token = TestAuthHelper.CreateToken(
            userId,
            UserRoles.Customer,
            configuration
        );

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                token
            );

        HttpRequestMessage request2 = new(
            HttpMethod.Post,
            $"/api/bank-accounts/{fromAccountId}/transfer"
        );

        request2.Headers.Add(
            "Idempotency-Key",
            idempotencyKey
        );

        request2.Content =
            JsonContent.Create(requestBody);

        Task<HttpResponseMessage> task1 =
            _client.SendAsync(request1);

        Task<HttpResponseMessage> task2 =
            _client.SendAsync(request2);

        HttpResponseMessage[] responses =
            await Task.WhenAll(
                task1,
                task2
            );

        Assert.All(
            responses,
            response =>
                Assert.True(
                    response.IsSuccessStatusCode
                )
        );

        dbContext.ChangeTracker.Clear();

        BankAccount updatedFromAccount =
            await dbContext.BankAccounts
                .SingleAsync(
                    account =>
                        account.Id == fromAccountId
                );

        BankAccount updatedToAccount =
            await dbContext.BankAccounts
                .SingleAsync(
                    account =>
                        account.Id == toAccountId
                );

        Assert.Equal(
            900m,
            updatedFromAccount.Balance
        );

        Assert.Equal(
            100m,
            updatedToAccount.Balance
        );
    }

    [Fact]
    public async Task Transfer_WithSameIdempotencyKeyButDifferentPayload_ShouldReturnConflict()
    {
        Guid userId = Guid.NewGuid();
        Guid fromAccountId = Guid.NewGuid();
        Guid toAccountId = Guid.NewGuid();

        using IServiceScope scope =
            _factory.Services.CreateScope();

        BankingDbContext dbContext =
            scope.ServiceProvider
                .GetRequiredService<BankingDbContext>();

        User user = new()
        {
            Id = userId,
            Email = $"test-{Guid.NewGuid()}@example.com",
            FullName = "Test User",
            CreatedAt = DateTime.UtcNow
        };

        BankAccount fromAccount = new()
        {
            Id = fromAccountId,
            UserId = userId,
            Iban = $"TEST-FROM-{Guid.NewGuid()}",
            Balance = 1000m
        };

        BankAccount toAccount = new()
        {
            Id = toAccountId,
            UserId = userId,
            Iban = $"TEST-TO-{Guid.NewGuid()}",
            Balance = 0m
        };

        dbContext.Users.Add(user);

        dbContext.BankAccounts.AddRange(
            fromAccount,
            toAccount
        );

        await dbContext.SaveChangesAsync();

        string idempotencyKey =
            Guid.NewGuid().ToString();
        IConfiguration configuration =
_factory.Services.GetRequiredService<IConfiguration>();

        string token = TestAuthHelper.CreateToken(
            userId,
            UserRoles.Customer,
            configuration
        );

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                token
            );

        HttpRequestMessage firstRequest = new(
            HttpMethod.Post,
            $"/api/bank-accounts/{fromAccountId}/transfer"
        );

        firstRequest.Headers.Add(
            "Idempotency-Key",
            idempotencyKey
        );

        firstRequest.Content = JsonContent.Create(
            new
            {
                toAccountId,
                amount = 100m
            }
        );

        HttpResponseMessage firstResponse =
            await _client.SendAsync(firstRequest);

        Assert.True(
            firstResponse.IsSuccessStatusCode
        );

        HttpRequestMessage secondRequest = new(
            HttpMethod.Post,
            $"/api/bank-accounts/{fromAccountId}/transfer"
        );

        secondRequest.Headers.Add(
            "Idempotency-Key",
            idempotencyKey
        );

        secondRequest.Content = JsonContent.Create(
            new
            {
                toAccountId,
                amount = 500m
            }
        );

        HttpResponseMessage secondResponse =
            await _client.SendAsync(secondRequest);

        Assert.Equal(
            HttpStatusCode.Conflict,
            secondResponse.StatusCode
        );

        dbContext.ChangeTracker.Clear();

        BankAccount updatedFromAccount =
            await dbContext.BankAccounts
                .SingleAsync(
                    account =>
                        account.Id == fromAccountId
                );

        BankAccount updatedToAccount =
            await dbContext.BankAccounts
                .SingleAsync(
                    account =>
                        account.Id == toAccountId
                );

        Assert.Equal(
            900m,
            updatedFromAccount.Balance
        );

        Assert.Equal(
            100m,
            updatedToAccount.Balance
        );

        int transactionCount =
            await dbContext.BankTransactions
                .CountAsync(
                    transaction =>
                        transaction.BankAccountId == fromAccountId ||
                        transaction.BankAccountId == toAccountId
                );

        Assert.Equal(
            2,
            transactionCount
        );

        int idempotencyCount =
            await dbContext.IdempotencyRecords
                .CountAsync(
                    record =>
                        record.Key == idempotencyKey &&
                        record.Operation ==
                            IdempotencyOperations.Transfer
                );

        Assert.Equal(
            1,
            idempotencyCount
        );
    }
}
