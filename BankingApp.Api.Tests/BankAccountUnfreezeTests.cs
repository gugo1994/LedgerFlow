using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using BankingApp.Api.Constants;
using BankingApp.Api.Data;
using BankingApp.Api.Entities;
using BankingApp.Api.Tests.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BankingApp.Api.Tests;

public sealed class BankAccountUnfreezeTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public BankAccountUnfreezeTests(
        CustomWebApplicationFactory factory
    )
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Admin_Can_Unfreeze_Account()
    {
        using IServiceScope scope =
            _factory.Services.CreateScope();

        BankingDbContext dbContext =
            scope.ServiceProvider
                .GetRequiredService<BankingDbContext>();

        User accountOwner =
            TestDataFactory.CreateUser(
                role: UserRoles.Customer
            );

        BankAccount account =
            TestDataFactory.CreateAccount(
                userId: accountOwner.Id,
                frozen: true
            );

        User adminUser =
            TestDataFactory.CreateUser(
                role: UserRoles.Admin
            );

        dbContext.Users.AddRange(
            accountOwner,
            adminUser
        );

        dbContext.BankAccounts.Add(account);

        await dbContext.SaveChangesAsync();

        IConfiguration configuration =
            _factory.Services
                .GetRequiredService<IConfiguration>();

        string token =
            TestAuthHelper.CreateToken(
                adminUser.Id,
                UserRoles.Admin,
                configuration
            );

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                token
            );

        using HttpRequestMessage request =
            new(
                HttpMethod.Patch,
                $"/api/bank-accounts/{account.Id}/unfreeze"
            );

        request.Headers.Add(
            "Idempotency-Key",
            Guid.NewGuid().ToString()
        );

        HttpResponseMessage response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode
        );

        BankAccount updatedAccount =
            await TestDbHelper.ReloadAccountAsync(
                dbContext,
                account.Id
            );

        Assert.False(updatedAccount.Frozen);
    }

    [Fact]
    public async Task Customer_Cannot_Unfreeze_Account()
    {
        using IServiceScope scope =
            _factory.Services.CreateScope();

        BankingDbContext dbContext =
            scope.ServiceProvider
                .GetRequiredService<BankingDbContext>();

        User accountOwner =
            TestDataFactory.CreateUser(
                role: UserRoles.Customer
            );

        BankAccount account =
            TestDataFactory.CreateAccount(
                userId: accountOwner.Id,
                frozen: true
            );

        User otherCustomer =
            TestDataFactory.CreateUser(
                role: UserRoles.Customer
            );

        dbContext.Users.AddRange(
            accountOwner,
            otherCustomer
        );

        dbContext.BankAccounts.Add(account);

        await dbContext.SaveChangesAsync();

        IConfiguration configuration =
            _factory.Services
                .GetRequiredService<IConfiguration>();

        string token =
            TestAuthHelper.CreateToken(
                otherCustomer.Id,
                UserRoles.Customer,
                configuration
            );

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                token
            );

        using HttpRequestMessage request =
            new(
                HttpMethod.Patch,
                $"/api/bank-accounts/{account.Id}/unfreeze"
            );

        request.Headers.Add(
            "Idempotency-Key",
            Guid.NewGuid().ToString()
        );

        HttpResponseMessage response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.Forbidden,
            response.StatusCode
        );

        BankAccount updatedAccount =
            await TestDbHelper.ReloadAccountAsync(
                dbContext,
                account.Id
            );

        Assert.True(updatedAccount.Frozen);
    }

    [Fact]
    public async Task Unfreeze_AlreadyActiveAccount_RemainsActive()
    {
        using IServiceScope scope =
            _factory.Services.CreateScope();

        BankingDbContext dbContext =
            scope.ServiceProvider
                .GetRequiredService<BankingDbContext>();

        User accountOwner =
            TestDataFactory.CreateUser(
                role: UserRoles.Customer
            );

        BankAccount account =
            TestDataFactory.CreateAccount(
                userId: accountOwner.Id,
                frozen: false
            );

        User adminUser =
            TestDataFactory.CreateUser(
                role: UserRoles.Admin
            );

        dbContext.Users.AddRange(
            accountOwner,
            adminUser
        );

        dbContext.BankAccounts.Add(account);

        await dbContext.SaveChangesAsync();

        IConfiguration configuration =
            _factory.Services
                .GetRequiredService<IConfiguration>();

        string token =
            TestAuthHelper.CreateToken(
                adminUser.Id,
                UserRoles.Admin,
                configuration
            );

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                token
            );

        using HttpRequestMessage request =
            new(
                HttpMethod.Patch,
                $"/api/bank-accounts/{account.Id}/unfreeze"
            );

        request.Headers.Add(
            "Idempotency-Key",
            Guid.NewGuid().ToString()
        );

        HttpResponseMessage response =
            await _client.SendAsync(request);

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode
        );

        BankAccount updatedAccount =
            await TestDbHelper.ReloadAccountAsync(
                dbContext,
                account.Id
            );

        Assert.False(updatedAccount.Frozen);
    }

    [Fact]
    public async Task Unfreeze_WithSameIdempotencyKey_ShouldNotFail()
    {
        using IServiceScope scope =
            _factory.Services.CreateScope();

        BankingDbContext dbContext =
            scope.ServiceProvider
                .GetRequiredService<BankingDbContext>();

        User accountOwner =
            TestDataFactory.CreateUser(
                role: UserRoles.Customer
            );

        BankAccount account =
            TestDataFactory.CreateAccount(
                userId: accountOwner.Id,
                frozen: true
            );

        User adminUser =
            TestDataFactory.CreateUser(
                role: UserRoles.Admin
            );

        dbContext.Users.AddRange(
            accountOwner,
            adminUser
        );

        dbContext.BankAccounts.Add(account);

        await dbContext.SaveChangesAsync();

        IConfiguration configuration =
            _factory.Services
                .GetRequiredService<IConfiguration>();

        string token =
            TestAuthHelper.CreateToken(
                adminUser.Id,
                UserRoles.Admin,
                configuration
            );

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                token
            );

        string idempotencyKey =
            Guid.NewGuid().ToString();

        using HttpRequestMessage firstRequest =
            new(
                HttpMethod.Patch,
                $"/api/bank-accounts/{account.Id}/unfreeze"
            );

        firstRequest.Headers.Add(
            "Idempotency-Key",
            idempotencyKey
        );

        using HttpRequestMessage secondRequest =
            new(
                HttpMethod.Patch,
                $"/api/bank-accounts/{account.Id}/unfreeze"
            );

        secondRequest.Headers.Add(
            "Idempotency-Key",
            idempotencyKey
        );

        HttpResponseMessage firstResponse =
            await _client.SendAsync(firstRequest);

        HttpResponseMessage secondResponse =
            await _client.SendAsync(secondRequest);

        Assert.Equal(
            HttpStatusCode.OK,
            firstResponse.StatusCode
        );

        Assert.Equal(
            HttpStatusCode.OK,
            secondResponse.StatusCode
        );

        BankAccount updatedAccount =
            await TestDbHelper.ReloadAccountAsync(
                dbContext,
                account.Id
            );

        Assert.False(updatedAccount.Frozen);
    }
}
