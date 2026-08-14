using System;

namespace BankingApp.Api.Exceptions;

public sealed class AccountFrozenException(string message) : Exception(message)
{
}
