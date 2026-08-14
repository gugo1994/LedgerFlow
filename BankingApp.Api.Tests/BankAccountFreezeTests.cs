using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using BankingApp.Api.Constants;
using BankingApp.Api.Data;
using BankingApp.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace BankingApp.Api.Tests;

public sealed class BankAccountFreezeTests
    : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public BankAccountFreezeTests(
        CustomWebApplicationFactory factory
    )
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Admin_Can_Freeze_Account()
    {
        Guid userId = Guid.NewGuid();
        Guid accountId = Guid.NewGuid();

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
            PasswordHash = "test",
            Role = "Customer",
            CreatedAt = DateTime.UtcNow
        };

        BankAccount account = new()
        {
            Id = accountId,
            UserId = userId,
            Iban = $"TEST-{Guid.NewGuid()}",
            Balance = 1000m,
            Frozen = false
        };

        dbContext.Users.Add(user);
        dbContext.BankAccounts.Add(account);


        Guid adminUserId = Guid.NewGuid();

        User adminUser = new()
{
    Id = adminUserId,
    Email = $"admin-{Guid.NewGuid()}@example.com",
    FullName = "Test Admin",
    PasswordHash = "test",
    Role = UserRoles.Admin,
    CreatedAt = DateTime.UtcNow
};

dbContext.Users.Add(adminUser);

await dbContext.SaveChangesAsync();

IConfiguration configuration =
    _factory.Services
        .GetRequiredService<IConfiguration>();

string jwtSecret =
    configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException(
        "Jwt:Secret is missing."
    );

string jwtIssuer =
    configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException(
        "Jwt:Issuer is missing."
    );

string jwtAudience =
    configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException(
        "Jwt:Audience is missing."
    );

string token = TestAuthHelper.CreateToken(
    adminUserId,
    UserRoles.Admin,
    jwtSecret,
    jwtIssuer,
    jwtAudience
);

_client.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue(
        "Bearer",
        token
    );

    HttpResponseMessage response =
    await _client.PatchAsync(
        $"/api/bank-accounts/{accountId}/freeze",
        content: null
    );

    Assert.Equal(
    HttpStatusCode.OK,
    response.StatusCode
);

dbContext.ChangeTracker.Clear();

BankAccount updatedAccount =
    await dbContext.BankAccounts
        .SingleAsync(
            account => account.Id == accountId
        );

Assert.True(updatedAccount.Frozen);
    }

    
}