using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using BankingApp.Api.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BankingApp.Api.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment
    )
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (InsufficientFundsException exception)
        {
            _logger.LogWarning(
                exception,
                "Insufficient funds error."
            );

            await WriteErrorAsync(
                context,
                HttpStatusCode.Conflict,
                exception.Message
            );
        }
        catch (IdempotencyConflictException exception)
        {
            _logger.LogWarning(
                exception,
                "Idempotency conflict."
            );

            await WriteErrorAsync(
                context,
                HttpStatusCode.Conflict,
                exception.Message
            );
        }
        catch (ArgumentException exception)
        {
            _logger.LogWarning(
                exception,
                "Invalid request."
            );

            await WriteErrorAsync(
                context,
                HttpStatusCode.BadRequest,
                exception.Message
            );
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Unhandled exception occurred."
            );

            string message = _environment.IsDevelopment()
             ? exception.Message
             : "An unexpected error occurred.";

            await WriteErrorAsync(
                context,
                HttpStatusCode.InternalServerError,
                message
            );
        }
    }

    private static async Task WriteErrorAsync(
        HttpContext context,
        HttpStatusCode statusCode,
        string message
    )
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";

        string json = JsonSerializer.Serialize(
            new
            {
                error = message
            }
        );

        await context.Response.WriteAsync(json);
    }
}
