using System;

namespace BankingApp.Api.Exceptions;

public sealed class ForbiddenException(string message) : Exception(message)
{
}
