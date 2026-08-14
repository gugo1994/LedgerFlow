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
                userId: accountOwner.Id
            );

        dbContext.Users.Add(accountOwner);
        dbContext.BankAccounts.Add(account);


        User adminUser = TestDataFactory.CreateUser(
            role: UserRoles.Admin
        );

        dbContext.Users.Add(adminUser);

        await dbContext.SaveChangesAsync();

        IConfiguration configuration =
            _factory.Services
                .GetRequiredService<IConfiguration>();
        string token = TestAuthHelper.CreateToken(
            adminUser.Id,
            UserRoles.Admin,
configuration
        );

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                token
            );

        HttpResponseMessage response =
        await _client.PatchAsync(
            $"/api/bank-accounts/{account.Id}/freeze",
            content: null
        );

        Assert.Equal(
        HttpStatusCode.OK,
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
    public async Task Customer_Cannot_Freeze_Account()
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
                userId: accountOwner.Id
            );


        dbContext.Users.Add(accountOwner);
        dbContext.BankAccounts.Add(account);


        User otherCustomer = TestDataFactory.CreateUser(
            role: UserRoles.Customer
        );

        dbContext.Users.Add(otherCustomer);

        await dbContext.SaveChangesAsync();

        IConfiguration configuration =
            _factory.Services
                .GetRequiredService<IConfiguration>();


        string token = TestAuthHelper.CreateToken(
            otherCustomer.Id,
            UserRoles.Customer,
configuration
        );

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                token
            );

        HttpResponseMessage response =
        await _client.PatchAsync(
            $"/api/bank-accounts/{account.Id}/freeze",
            content: null
        );

        Assert.Equal(
        HttpStatusCode.Forbidden,
        response.StatusCode
    );


        BankAccount updatedAccount =
            await TestDbHelper.ReloadAccountAsync(
                dbContext,
                account.Id
            );

        Assert.False(updatedAccount.Frozen);
    }


}
