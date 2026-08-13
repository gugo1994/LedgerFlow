using System;
using System.Collections.Generic;

namespace BankingApp.Api.Entities;

public class User
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public List<BankAccount> Accounts { get; set; } = new();
}
