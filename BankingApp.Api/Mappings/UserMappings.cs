using BankingApp.Api.DTOs;
using BankingApp.Api.Entities;

namespace BankingApp.Api.Mappings;

public static class UserMappings
{
    public static UserResponse ToResponse(this User user)
    {
        return new UserResponse(
            user.Id,
            user.Email,
            user.FullName,
            user.Role,
            user.CreatedAt
        );
    }
}
