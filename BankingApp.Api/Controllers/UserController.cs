using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BankingApp.Api.Constants;
using BankingApp.Api.DTOs;
using BankingApp.Api.Entities;
using BankingApp.Api.Mappings;
using BankingApp.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankingApp.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService, IAuthService authService)
    {
        _userService = userService;
    }

    [Authorize(Roles = UserRoles.Admin)]
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


    [Authorize]
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

    [Authorize]
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

    [Authorize]
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
