using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDuelAcceptanceAndRuns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AcceptBy",
                table: "Matches",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "FirstAccepted",
                table: "Matches",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SecondAccepted",
                table: "Matches",
                type: "boolean",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "Matches"
                SET "FirstAccepted" = ("StartedAt" IS NOT NULL),
                    "SecondAccepted" = ("StartedAt" IS NOT NULL)
                WHERE "Type" = 1;
                """);

            migrationBuilder.AddColumn<string>(
                name: "DuelPlayerId",
                table: "CodeRuns",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AcceptBy",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "FirstAccepted",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "SecondAccepted",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "DuelPlayerId",
                table: "CodeRuns");
        }
    }
}
