using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDuels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Submissions_MatchProblemId",
                table: "Submissions");

            migrationBuilder.AddColumn<string>(
                name: "DuelPlayerId",
                table: "Submissions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DurationSeconds",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FirstDelta",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FirstRating",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FirstSeenAt",
                table: "Matches",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OpponentId",
                table: "Matches",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Outcome",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Ranked",
                table: "Matches",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SecondDelta",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SecondRating",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SecondSeenAt",
                table: "Matches",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DuelQueue",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    EnqueuedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DuelQueue", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_DuelQueue_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_MatchProblemId",
                table: "Submissions",
                column: "MatchProblemId",
                unique: true,
                filter: "\"Status\" IN (0, 1, 2) AND \"DuelPlayerId\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_MatchProblemId_DuelPlayerId",
                table: "Submissions",
                columns: new[] { "MatchProblemId", "DuelPlayerId" },
                unique: true,
                filter: "\"Status\" IN (0, 1, 2) AND \"DuelPlayerId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Matches_OpponentId",
                table: "Matches",
                column: "OpponentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Matches_AspNetUsers_OpponentId",
                table: "Matches",
                column: "OpponentId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Matches_AspNetUsers_OpponentId",
                table: "Matches");

            migrationBuilder.DropTable(
                name: "DuelQueue");

            migrationBuilder.DropIndex(
                name: "IX_Submissions_MatchProblemId",
                table: "Submissions");

            migrationBuilder.DropIndex(
                name: "IX_Submissions_MatchProblemId_DuelPlayerId",
                table: "Submissions");

            migrationBuilder.DropIndex(
                name: "IX_Matches_OpponentId",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "DuelPlayerId",
                table: "Submissions");

            migrationBuilder.DropColumn(
                name: "DurationSeconds",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "FirstDelta",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "FirstRating",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "FirstSeenAt",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "OpponentId",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "Outcome",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "Ranked",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "SecondDelta",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "SecondRating",
                table: "Matches");

            migrationBuilder.DropColumn(
                name: "SecondSeenAt",
                table: "Matches");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_MatchProblemId",
                table: "Submissions",
                column: "MatchProblemId",
                unique: true,
                filter: "\"Status\" IN (0, 1, 2)");
        }
    }
}
