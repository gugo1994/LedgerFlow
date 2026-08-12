using System;

namespace BankingApp.Api.Exceptions;

public sealed class InsufficientFundsException : Exception
{
    public InsufficientFundsException()
        : base("Insufficient funds.")
    {
    }
}
