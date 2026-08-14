using System;

namespace BankingApp.Api.Entities;

public class BankAccount
{
    public Guid Id { get; set; }

    public string Iban { get; set; } = string.Empty;

    public decimal Balance { get; set; }

    public bool Frozen { get; set; } = false;

    public Guid UserId { get; set; }

    public User User { get; set; } = null!;
}
