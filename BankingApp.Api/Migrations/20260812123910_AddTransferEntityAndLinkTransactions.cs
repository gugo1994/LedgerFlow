using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankingApp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTransferEntityAndLinkTransactions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Nullable with no default: rows written before transfers were
            // modelled have no transfer to reference, and an all-zeros default
            // would fail the foreign key added at the end of this migration.
            migrationBuilder.AddColumn<Guid>(
                name: "TransferId",
                table: "BankTransactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Transfers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FromAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    ToAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transfers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Transfers_BankAccounts_FromAccountId",
                        column: x => x.FromAccountId,
                        principalTable: "BankAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Transfers_BankAccounts_ToAccountId",
                        column: x => x.ToAccountId,
                        principalTable: "BankAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BankTransactions_TransferId",
                table: "BankTransactions",
                column: "TransferId");

            migrationBuilder.CreateIndex(
                name: "IX_Transfers_FromAccountId",
                table: "Transfers",
                column: "FromAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Transfers_ToAccountId",
                table: "Transfers",
                column: "ToAccountId");

            migrationBuilder.AddForeignKey(
                name: "FK_BankTransactions_Transfers_TransferId",
                table: "BankTransactions",
                column: "TransferId",
                principalTable: "Transfers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BankTransactions_Transfers_TransferId",
                table: "BankTransactions");

            migrationBuilder.DropTable(
                name: "Transfers");

            migrationBuilder.DropIndex(
                name: "IX_BankTransactions_TransferId",
                table: "BankTransactions");

            migrationBuilder.DropColumn(
                name: "TransferId",
                table: "BankTransactions");
        }
    }
}
