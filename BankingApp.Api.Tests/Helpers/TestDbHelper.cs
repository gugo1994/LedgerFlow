using System;
using System.Threading.Tasks;
using BankingApp.Api.Data;
using BankingApp.Api.Entities;
using Microsoft.EntityFrameworkCore;

public static class TestDbHelper
{
    public static async Task<BankAccount> ReloadAccountAsync(
        BankingDbContext dbContext,
        Guid accountId
    )
    {
        dbContext.ChangeTracker.Clear();

        return await dbContext.BankAccounts
            .SingleAsync(
                account => account.Id == accountId
            );
    }
}
