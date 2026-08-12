using System.ComponentModel.DataAnnotations;

namespace BankingApp.Api.DTOs;

public sealed class UpdateUserRequest
{
    [Required]
    [MaxLength(100), MinLength(2)]
    public string FullName { get; init; } = string.Empty;
}
