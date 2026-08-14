using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankingApp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddFrozenToBankAccaunt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Frozen",
                table: "BankAccounts",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Frozen",
                table: "BankAccounts");
        }
    }
}
