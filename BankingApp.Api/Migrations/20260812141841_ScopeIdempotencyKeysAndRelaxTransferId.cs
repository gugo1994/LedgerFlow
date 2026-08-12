using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankingApp.Api.Migrations
{
    /// <inheritdoc />
    public partial class ScopeIdempotencyKeysAndRelaxTransferId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_IdempotencyRecords_Key_Operation",
                table: "IdempotencyRecords");

            migrationBuilder.AddColumn<string>(
                name: "Scope",
                table: "IdempotencyRecords",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<Guid>(
                name: "TransferId",
                table: "BankTransactions",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            // AlterColumn only drops NOT NULL; the all-zeros default added by
            // AddTransferEntityAndLinkTransactions survives it and would make
            // every insert without a transfer violate the foreign key.
            migrationBuilder.Sql(
                """
                ALTER TABLE "BankTransactions"
                ALTER COLUMN "TransferId" DROP DEFAULT;
                """);

            migrationBuilder.Sql(
                """
                UPDATE "BankTransactions"
                SET "TransferId" = NULL
                WHERE "TransferId" = '00000000-0000-0000-0000-000000000000';
                """);

            migrationBuilder.CreateIndex(
                name: "UX_IdempotencyRecords_Scope_Key_Operation",
                table: "IdempotencyRecords",
                columns: new[] { "Scope", "Key", "Operation" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_IdempotencyRecords_Scope_Key_Operation",
                table: "IdempotencyRecords");

            migrationBuilder.DropColumn(
                name: "Scope",
                table: "IdempotencyRecords");

            migrationBuilder.AlterColumn<Guid>(
                name: "TransferId",
                table: "BankTransactions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "UX_IdempotencyRecords_Key_Operation",
                table: "IdempotencyRecords",
                columns: new[] { "Key", "Operation" },
                unique: true);
        }
    }
}
