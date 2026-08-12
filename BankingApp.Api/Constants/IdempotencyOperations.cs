namespace BankingApp.Api.Constants;

public static class IdempotencyOperations
{
    public const string Transfer = "Transfer";

    public const string UniqueIndexName =
        "UX_IdempotencyRecords_Scope_Key_Operation";
}
