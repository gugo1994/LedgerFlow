using System.Threading;
using System.Threading.Tasks;
using BankingApp.Api.DTOs;
using BankingApp.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace BankingApp.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    private readonly IAuthService _authService = authService;

    [HttpPost("register")]
    public async Task<ActionResult<UserResponse>> CreateUser(
        RegisterRequest request,
        CancellationToken cancellationToken
    )
    {
        UserResponse userResponse = await _authService.RegisterAsync(
            request,
            cancellationToken
        );

        return Ok(userResponse);
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken
    )
    {
        LoginResponse loginResponse = await _authService.LoginAsync(
            request,
            cancellationToken
        );

        return Ok(loginResponse);
    }
}
