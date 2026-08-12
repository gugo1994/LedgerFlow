using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankingApp.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateIdempotencyUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_IdempotencyRecords_Key",
                table: "IdempotencyRecords");

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyRecords_Key_Operation",
                table: "IdempotencyRecords",
                columns: new[] { "Key", "Operation" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_IdempotencyRecords_Key_Operation",
                table: "IdempotencyRecords");

            migrationBuilder.CreateIndex(
                name: "IX_IdempotencyRecords_Key",
                table: "IdempotencyRecords",
                column: "Key",
                unique: true);
        }
    }
}
