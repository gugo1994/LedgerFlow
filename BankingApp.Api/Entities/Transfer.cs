using System;

namespace BankingApp.Api.Entities;

public class Transfer
{
    public Guid Id { get; set; }

    public Guid FromAccountId { get; set; }

    public Guid ToAccountId { get; set; }

    public decimal Amount { get; set; }

    public DateTime CreatedAt { get; set; }

    public BankAccount FromAccount { get; set; } = null!;

    public BankAccount ToAccount { get; set; } = null!;
}
