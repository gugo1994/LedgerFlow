using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankingApp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddIdempotencyResponse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ResponseBody",
                table: "IdempotencyRecords",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ResponseStatusCode",
                table: "IdempotencyRecords",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResponseBody",
                table: "IdempotencyRecords");

            migrationBuilder.DropColumn(
                name: "ResponseStatusCode",
                table: "IdempotencyRecords");
        }
    }
}
