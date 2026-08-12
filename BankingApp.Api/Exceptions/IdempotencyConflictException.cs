using System;

namespace BankingApp.Api.Exceptions;

public sealed class IdempotencyConflictException : Exception
{
    public IdempotencyConflictException()
        : base("The same idempotency key was used with a different request.")
    {
    }
}
