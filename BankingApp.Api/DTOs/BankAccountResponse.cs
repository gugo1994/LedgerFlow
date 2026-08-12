using System;

namespace BankingApp.Api.DTOs;

public sealed record BankAccountResponse(
    Guid Id,
    string Iban,
    decimal Balance,
    Guid UserId,
    UserResponse User
);
