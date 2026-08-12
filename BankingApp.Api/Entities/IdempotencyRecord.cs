using System;

namespace BankingApp.Api.Entities;

public class IdempotencyRecord
{
    public Guid Id { get; set; }

    public string Scope { get; set; } = string.Empty;

    public string Key { get; set; } = string.Empty;

    public string Operation { get; set; } = string.Empty;

    public string RequestHash { get; set; } = string.Empty;

    public int ResponseStatusCode { get; set; }

    public string? ResponseBody { get; set; }

    public DateTime CreatedAt { get; set; }
}
