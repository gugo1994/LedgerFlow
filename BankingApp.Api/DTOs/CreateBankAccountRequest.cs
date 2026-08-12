using System;
using System.ComponentModel.DataAnnotations;

namespace BankingApp.Api.DTOs;

public sealed class CreateBankAccountRequest
{
    [Required]
    public Guid UserId { get; init; }

    [Required]
    public string Iban { get; init; } = string.Empty;
}
