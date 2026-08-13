using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BankingApp.Api.Data;
using BankingApp.Api.DTOs;
using BankingApp.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace BankingApp.Api.Services;

public sealed class UserService : IUserService
{
    private readonly BankingDbContext _dbContext;

    public UserService(BankingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<User?> GetByIdAsync(
    Guid id,
    CancellationToken cancellationToken
)
    {
        return await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                user => user.Id == id,
                cancellationToken
            );
    }
    public async Task<List<UserResponse>> GetAllAsync(
        CancellationToken cancellationToken
    )
    {
        return await _dbContext.Users
            .AsNoTracking()
            .Select(user => new UserResponse(
                user.Id,
                user.Email,
                user.FullName,
                user.CreatedAt
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<User?> UpdateAsync(
        Guid id,
        string fullName,
        CancellationToken cancellationToken
    )
    {
        User? user = await _dbContext.Users
            .FirstOrDefaultAsync(
                user => user.Id == id,
                cancellationToken
            );

        if (user is null)
        {
            return null;
        }

        user.FullName = fullName;

        await _dbContext.SaveChangesAsync(
            cancellationToken
        );

        return user;
    }
    public async Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken
    )
    {
        User? user = await _dbContext.Users
            .FirstOrDefaultAsync(
                user => user.Id == id,
                cancellationToken
            );

        if (user is null)
        {
            return false;
        }

        _dbContext.Users.Remove(user);

        await _dbContext.SaveChangesAsync(
            cancellationToken
        );

        return true;
    }
}
