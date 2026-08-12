using BankingApp.Api.DTOs;
using BankingApp.Api.Entities;

namespace BankingApp.Api.Mappings;

public static class BankAccountMappings
{

    public static BankAccountResponse ToResponse(
        this BankAccount account
    )
    {
        return new BankAccountResponse(
            account.Id,
            account.Iban,
            account.Balance,
            account.UserId,
              new UserResponse(
                account.User.Id,
                account.User.Email,
                account.User.FullName,
                account.User.CreatedAt
            )
        );
    }
}
