using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using BankingApp.Api.DTOs;
using BankingApp.Api.Entities;
using BankingApp.Api.Mappings;
using BankingApp.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankingApp.Api.Controllers;

[ApiController]
[Route("api/bank-accounts")]
public sealed class BankAccountsController : ControllerBase
{
    private readonly IBankAccountService _bankAccountService;

    public BankAccountsController(
        IBankAccountService bankAccountService
    )
    {
        _bankAccountService = bankAccountService;
    }

    [Authorize]
    [HttpPost]
    public async Task<ActionResult<BankAccountResponse>> Create(
        CreateBankAccountRequest request,
        CancellationToken cancellationToken
    )
    {
        BankAccount? account =
            await _bankAccountService.CreateAsync(
                request.UserId,
                request.Iban,
                cancellationToken
            );

        if (account is null)
        {
            return NotFound(
                "User not found."
            );
        }

        return Ok(account.ToResponse());
    }
    [Authorize]
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BankAccountResponse>> GetById(
    Guid id,
    CancellationToken cancellationToken
)
    {
        BankAccount? account =
            await _bankAccountService.GetByIdAsync(
                id,
                cancellationToken
            );

        if (account is null)
        {
            return NotFound();
        }

        return Ok(account.ToResponse());
    }

    [Authorize]
    [HttpPost("{fromAccountId:guid}/transfer")]
    public async Task<IActionResult> Transfer(
        Guid fromAccountId,
        TransferRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken
    )
    {
        Guid currentUserId =
       GetCurrentUserId();

        TransferResult result = await _bankAccountService.TransferAsync(
            currentUserId,
            fromAccountId,
            request.ToAccountId,
            request.Amount,
            idempotencyKey,
            cancellationToken
        );

        return StatusCode(
          result.StatusCode,
          new
          {
              success = result.Success,
              message = result.ResponseBody,
              isReplay = result.IsReplay
          }
      );
    }

    private Guid GetCurrentUserId()
    {
        string? userIdValue =
            User.FindFirstValue(
                JwtRegisteredClaimNames.Sub
            );
        if (
            userIdValue is null ||
            !Guid.TryParse(userIdValue, out Guid userId)
        )
        {
            throw new UnauthorizedAccessException(
                "Invalid user identity."
            );
        }

        return userId;
    }
}
