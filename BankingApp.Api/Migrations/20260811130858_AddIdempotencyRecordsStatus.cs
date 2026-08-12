using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BankingApp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddIdempotencyRecordsStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsCompleted",
                table: "IdempotencyRecords");

            migrationBuilder.AddColumn<DateTime>(
                name: "CompletedAt",
                table: "IdempotencyRecords",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "IdempotencyRecords",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompletedAt",
                table: "IdempotencyRecords");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "IdempotencyRecords");

            migrationBuilder.AddColumn<bool>(
                name: "IsCompleted",
                table: "IdempotencyRecords",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
