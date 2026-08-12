using System.ComponentModel.DataAnnotations;

namespace BankingApp.Api.DTOs;

public sealed class CreateUserRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    [MaxLength(100), MinLength(2)]
    public string FullName { get; init; } = string.Empty;
}
