using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCodeRuns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Problems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CreatedByUserId = table.Column<string>(type: "text", nullable: false),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Problems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Problems_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProgrammingLanguages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    FileExtension = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RuntimeVersion = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgrammingLanguages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProblemVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProblemId = table.Column<Guid>(type: "uuid", nullable: false),
                    VersionNumber = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    DescriptionMarkdown = table.Column<string>(type: "text", nullable: false),
                    InputDescriptionMarkdown = table.Column<string>(type: "text", nullable: true),
                    OutputDescriptionMarkdown = table.Column<string>(type: "text", nullable: true),
                    ConstraintsMarkdown = table.Column<string>(type: "text", nullable: true),
                    ExplanationMarkdown = table.Column<string>(type: "text", nullable: true),
                    Difficulty = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProblemVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProblemVersions_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProblemVersions_Problems_ProblemId",
                        column: x => x.ProblemId,
                        principalTable: "Problems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExecutionJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProgrammingLanguageId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceCode = table.Column<string>(type: "text", nullable: false),
                    StandardInput = table.Column<string>(type: "text", nullable: false),
                    CpuTimeLimitSeconds = table.Column<double>(type: "double precision", nullable: false),
                    WallTimeLimitSeconds = table.Column<double>(type: "double precision", nullable: false),
                    MemoryLimitKilobytes = table.Column<int>(type: "integer", nullable: false),
                    OutputLimitKilobytes = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RetryOfJobId = table.Column<Guid>(type: "uuid", nullable: true),
                    QueuedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    FailureCode = table.Column<string>(type: "text", nullable: true),
                    FailureMessage = table.Column<string>(type: "text", nullable: true),
                    Result_Verdict = table.Column<int>(type: "integer", nullable: true),
                    Result_StandardOutput = table.Column<string>(type: "text", nullable: true),
                    Result_StandardError = table.Column<string>(type: "text", nullable: true),
                    Result_CompilationOutput = table.Column<string>(type: "text", nullable: true),
                    Result_DiagnosticMessage = table.Column<string>(type: "text", nullable: true),
                    Result_ExitCode = table.Column<int>(type: "integer", nullable: true),
                    Result_ExitSignal = table.Column<int>(type: "integer", nullable: true),
                    Result_CpuTimeSeconds = table.Column<double>(type: "double precision", nullable: true),
                    Result_WallTimeSeconds = table.Column<double>(type: "double precision", nullable: true),
                    Result_MemoryKilobytes = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExecutionJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExecutionJobs_ExecutionJobs_RetryOfJobId",
                        column: x => x.RetryOfJobId,
                        principalTable: "ExecutionJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExecutionJobs_ProgrammingLanguages_ProgrammingLanguageId",
                        column: x => x.ProgrammingLanguageId,
                        principalTable: "ProgrammingLanguages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MatchProblems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProblemVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    SubmissionDeadlineAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ClosedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CloseReason = table.Column<int>(type: "integer", nullable: true),
                    FinalizedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Outcome = table.Column<int>(type: "integer", nullable: true),
                    FirstAcceptedSubmissionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MatchProblems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MatchProblems_Matches_MatchId",
                        column: x => x.MatchId,
                        principalTable: "Matches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MatchProblems_ProblemVersions_ProblemVersionId",
                        column: x => x.ProblemVersionId,
                        principalTable: "ProblemVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProblemLanguages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProblemVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProgrammingLanguageId = table.Column<Guid>(type: "uuid", nullable: false),
                    Template = table.Column<string>(type: "text", nullable: false),
                    CpuTimeLimitSeconds = table.Column<double>(type: "double precision", nullable: false),
                    WallTimeLimitSeconds = table.Column<double>(type: "double precision", nullable: false),
                    MemoryLimitKilobytes = table.Column<int>(type: "integer", nullable: false),
                    OutputLimitKilobytes = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProblemLanguages", x => x.Id);
                    table.CheckConstraint("CK_ProblemLanguages_PositiveLimits", "\"CpuTimeLimitSeconds\" > 0 AND \"WallTimeLimitSeconds\" > 0 AND \"MemoryLimitKilobytes\" > 0 AND \"OutputLimitKilobytes\" > 0");
                    table.ForeignKey(
                        name: "FK_ProblemLanguages_ProblemVersions_ProblemVersionId",
                        column: x => x.ProblemVersionId,
                        principalTable: "ProblemVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProblemLanguages_ProgrammingLanguages_ProgrammingLanguageId",
                        column: x => x.ProgrammingLanguageId,
                        principalTable: "ProgrammingLanguages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProblemTestCases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProblemVersionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    Input = table.Column<string>(type: "text", nullable: false),
                    ExpectedOutput = table.Column<string>(type: "text", nullable: false),
                    IsSample = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProblemTestCases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProblemTestCases_ProblemVersions_ProblemVersionId",
                        column: x => x.ProblemVersionId,
                        principalTable: "ProblemVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CodeRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MatchProblemId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExecutionJobId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodeRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CodeRuns_ExecutionJobs_ExecutionJobId",
                        column: x => x.ExecutionJobId,
                        principalTable: "ExecutionJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CodeRuns_MatchProblems_MatchProblemId",
                        column: x => x.MatchProblemId,
                        principalTable: "MatchProblems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CodeRuns_ExecutionJobId",
                table: "CodeRuns",
                column: "ExecutionJobId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CodeRuns_MatchProblemId_CreatedAt",
                table: "CodeRuns",
                columns: new[] { "MatchProblemId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionJobs_ProgrammingLanguageId",
                table: "ExecutionJobs",
                column: "ProgrammingLanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionJobs_RetryOfJobId",
                table: "ExecutionJobs",
                column: "RetryOfJobId");

            migrationBuilder.CreateIndex(
                name: "IX_ExecutionJobs_Status_CreatedAt",
                table: "ExecutionJobs",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MatchProblems_MatchId_Position",
                table: "MatchProblems",
                columns: new[] { "MatchId", "Position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MatchProblems_MatchId_ProblemVersionId",
                table: "MatchProblems",
                columns: new[] { "MatchId", "ProblemVersionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MatchProblems_ProblemVersionId",
                table: "MatchProblems",
                column: "ProblemVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_ProblemLanguages_ProblemVersionId_ProgrammingLanguageId",
                table: "ProblemLanguages",
                columns: new[] { "ProblemVersionId", "ProgrammingLanguageId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProblemLanguages_ProgrammingLanguageId",
                table: "ProblemLanguages",
                column: "ProgrammingLanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_Problems_CreatedByUserId",
                table: "Problems",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Problems_Key",
                table: "Problems",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProblemTestCases_ProblemVersionId_Position",
                table: "ProblemTestCases",
                columns: new[] { "ProblemVersionId", "Position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProblemVersions_CreatedByUserId",
                table: "ProblemVersions",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProblemVersions_ProblemId_VersionNumber",
                table: "ProblemVersions",
                columns: new[] { "ProblemId", "VersionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProgrammingLanguages_Key",
                table: "ProgrammingLanguages",
                column: "Key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CodeRuns");

            migrationBuilder.DropTable(
                name: "ProblemLanguages");

            migrationBuilder.DropTable(
                name: "ProblemTestCases");

            migrationBuilder.DropTable(
                name: "ExecutionJobs");

            migrationBuilder.DropTable(
                name: "MatchProblems");

            migrationBuilder.DropTable(
                name: "ProgrammingLanguages");

            migrationBuilder.DropTable(
                name: "ProblemVersions");

            migrationBuilder.DropTable(
                name: "Problems");
        }
    }
}
