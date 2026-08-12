using System;
using System.Threading;
using System.Threading.Tasks;
using BankingApp.Api.DTOs;
using BankingApp.Api.Entities;
using BankingApp.Api.Mappings;
using BankingApp.Api.Services;
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

    [HttpPost("{id:guid}/transfer")]
    public async Task<IActionResult> Transfer(
        Guid id,
        TransferRequest request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey,
        CancellationToken cancellationToken
    )
    {
        TransferResult result = await _bankAccountService.TransferAsync(
            id,
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
}
