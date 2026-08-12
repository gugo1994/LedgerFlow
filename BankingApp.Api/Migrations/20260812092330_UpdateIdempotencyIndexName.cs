using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankingApp.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateIdempotencyIndexName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_IdempotencyRecords_Key_Operation",
                table: "IdempotencyRecords",
                newName: "UX_IdempotencyRecords_Key_Operation");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "UX_IdempotencyRecords_Key_Operation",
                table: "IdempotencyRecords",
                newName: "IX_IdempotencyRecords_Key_Operation");
        }
    }
}
