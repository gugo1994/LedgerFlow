using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BankingApp.Api.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace BankingApp.Api.Services;

public sealed class JwtTokenService(
    IConfiguration configuration
    )
        : IJwtTokenService
{
    private readonly IConfiguration _configuration = configuration;

    public string GenerateAccessToken(User user)
    {
        string secret =
            _configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException(
                "JWT secret is missing."
            );

        string issuer =
            _configuration["Jwt:Issuer"]
            ?? throw new InvalidOperationException(
                "JWT issuer is missing."
            );

        string audience =
            _configuration["Jwt:Audience"]
            ?? throw new InvalidOperationException(
                "JWT audience is missing."
            );

        Claim[] claims =
        [
            new Claim(
                JwtRegisteredClaimNames.Sub,
                user.Id.ToString()
            ),

            new Claim(
                JwtRegisteredClaimNames.Email,
                user.Email
            ),

            new Claim(
                "full_name",
                user.FullName
            )
        ];

        SymmetricSecurityKey key =
            new(
                Encoding.UTF8.GetBytes(secret)
            );

        SigningCredentials credentials =
            new(
                key,
                SecurityAlgorithms.HmacSha256
            );

        JwtSecurityToken token = new(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(60),
            signingCredentials: credentials
        );

        JwtSecurityTokenHandler handler = new();
        return handler.WriteToken(token);
    }
}
