using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BankingApp.Api.DTOs;
using BankingApp.Api.Entities;

namespace BankingApp.Api.Services;

public interface IUserService
{
    Task<User?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken
    );

    Task<List<UserResponse>> GetAllAsync(
        CancellationToken cancellationToken
    );

    Task<User?> UpdateAsync(
        Guid id,
        string fullName,
        CancellationToken cancellationToken
    );

    Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken
    );
}
