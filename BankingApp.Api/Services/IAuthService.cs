using System.Threading;
using System.Threading.Tasks;
using BankingApp.Api.DTOs;

namespace BankingApp.Api.Services;

public interface IAuthService
{
    Task<UserResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken
    );

    Task<LoginResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken
    );
}
