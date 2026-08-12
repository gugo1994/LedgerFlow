using BankingApp.Api.Constants;
using BankingApp.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankingApp.Api.Data;

public sealed class BankingDbContext : DbContext
{
    public BankingDbContext(
        DbContextOptions<BankingDbContext> options
    ) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<BankAccount> BankAccounts =>
        Set<BankAccount>();

    public DbSet<BankTransaction> BankTransactions =>
        Set<BankTransaction>();

    public DbSet<IdempotencyRecord> IdempotencyRecords =>
        Set<IdempotencyRecord>();

    public DbSet<Transfer> Transfers =>
       Set<Transfer>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder
    )
    {
        modelBuilder.Entity<BankAccount>()
            .HasOne(account => account.User)
            .WithMany(user => user.Accounts)
            .HasForeignKey(account => account.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<BankTransaction>()
            .HasOne(transaction => transaction.BankAccount)
            .WithMany()
    .HasForeignKey(transaction => transaction.BankAccountId)
    .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Transfer>()
            .HasOne(transfer => transfer.FromAccount)
            .WithMany()
            .HasForeignKey(transfer => transfer.FromAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Transfer>()
            .HasOne(transfer => transfer.ToAccount)
            .WithMany()
            .HasForeignKey(transfer => transfer.ToAccountId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<IdempotencyRecord>()
            .HasIndex(record => new
            {
                record.Scope,
                record.Key,
                record.Operation
            })
            .IsUnique()
            .HasDatabaseName(IdempotencyOperations.UniqueIndexName);

        modelBuilder.Entity<BankTransaction>()
            .HasOne(transaction => transaction.Transfer)
            .WithMany()
            .HasForeignKey(transaction => transaction.TransferId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
