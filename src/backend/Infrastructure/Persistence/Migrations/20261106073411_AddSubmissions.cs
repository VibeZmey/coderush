using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSubmissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Submissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProgrammingLanguageId = table.Column<int>(type: "integer", nullable: false),
                    MatchProblemId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceCode = table.Column<string>(type: "text", nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Verdict = table.Column<int>(type: "integer", nullable: true),
                    FailureCode = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Submissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Submissions_MatchProblems_MatchProblemId",
                        column: x => x.MatchProblemId,
                        principalTable: "MatchProblems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Submissions_ProgrammingLanguages_ProgrammingLanguageId",
                        column: x => x.ProgrammingLanguageId,
                        principalTable: "ProgrammingLanguages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SubmissionTestResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProblemTestCaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExecutionJobId = table.Column<Guid>(type: "uuid", nullable: false),
                    Verdict = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubmissionTestResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubmissionTestResults_ExecutionJobs_ExecutionJobId",
                        column: x => x.ExecutionJobId,
                        principalTable: "ExecutionJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubmissionTestResults_ProblemTestCases_ProblemTestCaseId",
                        column: x => x.ProblemTestCaseId,
                        principalTable: "ProblemTestCases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubmissionTestResults_Submissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "Submissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MatchProblems_FirstAcceptedSubmissionId",
                table: "MatchProblems",
                column: "FirstAcceptedSubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_MatchProblemId",
                table: "Submissions",
                column: "MatchProblemId",
                unique: true,
                filter: "\"Status\" IN (0, 1, 2)");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_ProgrammingLanguageId",
                table: "Submissions",
                column: "ProgrammingLanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_Status_SubmittedAt",
                table: "Submissions",
                columns: new[] { "Status", "SubmittedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionTestResults_ExecutionJobId",
                table: "SubmissionTestResults",
                column: "ExecutionJobId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionTestResults_ProblemTestCaseId",
                table: "SubmissionTestResults",
                column: "ProblemTestCaseId");

            migrationBuilder.CreateIndex(
                name: "IX_SubmissionTestResults_SubmissionId_ProblemTestCaseId",
                table: "SubmissionTestResults",
                columns: new[] { "SubmissionId", "ProblemTestCaseId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_MatchProblems_Submissions_FirstAcceptedSubmissionId",
                table: "MatchProblems",
                column: "FirstAcceptedSubmissionId",
                principalTable: "Submissions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MatchProblems_Submissions_FirstAcceptedSubmissionId",
                table: "MatchProblems");

            migrationBuilder.DropTable(
                name: "SubmissionTestResults");

            migrationBuilder.DropTable(
                name: "Submissions");

            migrationBuilder.DropIndex(
                name: "IX_MatchProblems_FirstAcceptedSubmissionId",
                table: "MatchProblems");
        }
    }
}
