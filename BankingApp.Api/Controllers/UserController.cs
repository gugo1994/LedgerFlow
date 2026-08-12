using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BankingApp.Api.DTOs;
using BankingApp.Api.Entities;
using BankingApp.Api.Mappings;
using BankingApp.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace BankingApp.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }
    [HttpGet]
    public async Task<ActionResult<List<UserResponse>>> GetUsers(
    CancellationToken cancellationToken
)
    {
        List<UserResponse> users =
            await _userService.GetAllAsync(
                cancellationToken
            );

        return Ok(users);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserResponse>> GetUser(
    Guid id,
    CancellationToken cancellationToken
)
    {
        User? user = await _userService.GetByIdAsync(
            id,
            cancellationToken
        );

        if (user is null)
        {
            return NotFound();
        }

        return Ok(user.ToResponse());
    }

    [HttpPost]
    public async Task<ActionResult<UserResponse>> CreateUser(
        CreateUserRequest request,
        CancellationToken cancellationToken
    )
    {
        User user = await _userService.CreateAsync(
            request.Email,
            request.FullName,
            cancellationToken
        );

        return Ok(user.ToResponse());
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UserResponse>> UpdateUser(
    Guid id,
    UpdateUserRequest request,
    CancellationToken cancellationToken
)
    {
        User? user = await _userService.UpdateAsync(
            id,
            request.FullName,
            cancellationToken
        );



        if (user is null)
        {
            return NotFound();
        }
        return Ok(user.ToResponse());
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteUser(
        Guid id,
        CancellationToken cancellationToken
    )
    {
        bool deleted = await _userService.DeleteAsync(
            id,
            cancellationToken
        );

        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}
