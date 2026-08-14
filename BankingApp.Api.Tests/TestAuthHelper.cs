using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace BankingApp.Api.Tests;

public static class TestAuthHelper
{
    public static string CreateToken(
        Guid userId,
        string role,
        string secret,
        string issuer,
        string audience
    )
    {
        Claim[] claims =
        [
            new Claim(
                JwtRegisteredClaimNames.Sub,
                userId.ToString()
            ),
            new Claim(
                ClaimTypes.Role,
                role
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
            expires: DateTime.UtcNow.AddMinutes(30),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler()
            .WriteToken(token);
    }
}