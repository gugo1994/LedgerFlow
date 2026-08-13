using System;
using System.Threading;
using System.Threading.Tasks;
using BankingApp.Api.Data;
using BankingApp.Api.DTOs;
using BankingApp.Api.Entities;
using BankingApp.Api.Mappings;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BankingApp.Api.Services;

public sealed class AuthService(
    BankingDbContext dbContext,
    IPasswordHasher<User> passwordHasher,
    IJwtTokenService jwtTokenService
) : IAuthService
{
    private readonly BankingDbContext _dbContext = dbContext;
    private readonly IPasswordHasher<User> _passwordHasher = passwordHasher;

    private readonly IJwtTokenService _jwtTokenService = jwtTokenService;

    public async Task<UserResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken
    )
    {
        bool emailExists =
            await _dbContext.Users.AnyAsync(
                user => user.Email == request.Email,
                cancellationToken
            );

        if (emailExists)
        {
            throw new InvalidOperationException(
                "A user with this email already exists."
            );
        }

        User user = new()
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            FullName = request.FullName,
            CreatedAt = DateTime.UtcNow
        };

        user.PasswordHash =
            _passwordHasher.HashPassword(
                user,
                request.Password
            );

        _dbContext.Users.Add(user);

        await _dbContext.SaveChangesAsync(
            cancellationToken
        );
        return user.ToResponse();
    }

    public async Task<LoginResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken
    )
    {
        User? user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                user => user.Email == request.Email,
                cancellationToken
            );

        if (user is null)
        {
            throw new InvalidOperationException(
                "User Not Found."
            );
        }

        PasswordVerificationResult result =
            _passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                request.Password
            );

        if (result != PasswordVerificationResult.Success)
        {
            throw new InvalidOperationException(
                "Wrong password."
            );
        }

        // Generate JWT token
        string token =
      _jwtTokenService.GenerateAccessToken(user);
        DateTime expiration = DateTime.UtcNow.AddHours(1);

        return new LoginResponse(token, expiration);
    }
}
