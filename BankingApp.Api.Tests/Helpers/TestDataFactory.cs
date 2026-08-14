using System;
using BankingApp.Api.Constants;
using BankingApp.Api.Entities;

namespace BankingApp.Api.Tests.Helpers;

public static class TestDataFactory
{
    public static User CreateUser(
        string role = UserRoles.Customer
    )
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Email = $"test-{Guid.NewGuid()}@example.com",
            FullName = "Test User",
            PasswordHash = "test",
            Role = role,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static BankAccount CreateAccount(
        Guid userId,
        decimal balance = 1000m,
        bool frozen = false
    )
    {
        return new BankAccount
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Iban = $"TEST-{Guid.NewGuid()}",
            Balance = balance,
            Frozen = frozen
        };
    }
}
