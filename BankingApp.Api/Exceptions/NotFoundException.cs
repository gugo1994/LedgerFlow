using System;

namespace BankingApp.Api.Exceptions;

public sealed class NotFoundException(string message) : Exception(message)
{
}
