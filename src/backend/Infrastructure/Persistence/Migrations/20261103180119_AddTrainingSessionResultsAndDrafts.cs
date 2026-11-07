using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTrainingSessionResultsAndDrafts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FinishReason",
                table: "Matches",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CodeDrafts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProgrammingLanguageId = table.Column<int>(type: "integer", nullable: false),
                    MatchProblemId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceCode = table.Column<string>(type: "text", nullable: false),
                    CustomInput = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodeDrafts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CodeDrafts_MatchProblems_MatchProblemId",
                        column: x => x.MatchProblemId,
                        principalTable: "MatchProblems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CodeDrafts_ProgrammingLanguages_ProgrammingLanguageId",
                        column: x => x.ProgrammingLanguageId,
                        principalTable: "ProgrammingLanguages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CodeDrafts_MatchProblemId_ProgrammingLanguageId",
                table: "CodeDrafts",
                columns: new[] { "MatchProblemId", "ProgrammingLanguageId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CodeDrafts_ProgrammingLanguageId",
                table: "CodeDrafts",
                column: "ProgrammingLanguageId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CodeDrafts");

            migrationBuilder.DropColumn(
                name: "FinishReason",
                table: "Matches");
        }
    }
}
