using BankingApp.Api.Entities;

namespace BankingApp.Api.Services;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user);
}
