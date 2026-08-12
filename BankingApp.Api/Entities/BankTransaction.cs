using System;

namespace BankingApp.Api.Entities;

public class BankTransaction
{
    public Guid Id { get; set; }

    public Guid BankAccountId { get; set; }

    public Guid? TransferId { get; set; }

    public decimal Amount { get; set; }

    public DateTime CreatedAt { get; set; }

    public BankAccount BankAccount { get; set; } = null!;

    public Transfer? Transfer { get; set; }
}
