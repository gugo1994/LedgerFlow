using System;

namespace BankingApp.Api.DTOs;

public sealed record UserResponse(
    Guid Id,
    string Email,
    string FullName,
    string Role,
    DateTime CreatedAt
);
