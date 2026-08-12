using System;
using System.ComponentModel.DataAnnotations;

namespace BankingApp.Api.DTOs;

public sealed class TransferRequest
{
    [Range(0.01, 1_000_000)]
    public decimal Amount { get; init; }

    [Required]
    public Guid ToAccountId { get; init; }
}
