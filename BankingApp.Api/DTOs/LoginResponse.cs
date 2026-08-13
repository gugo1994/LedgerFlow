using System;

namespace BankingApp.Api.DTOs;

public sealed record LoginResponse(
    string Token,
    DateTime Expiration
);
