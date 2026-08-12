namespace BankingApp.Api.DTOs;

public sealed record TransferResult(
    bool Success,
    bool IsReplay,
    int StatusCode,
    string? ResponseBody
);
