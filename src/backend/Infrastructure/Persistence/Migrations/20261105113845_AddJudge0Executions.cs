using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddJudge0Executions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Judge0Executions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExecutionJobId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmissionToken = table.Column<string>(type: "text", nullable: true),
                    LastStatusId = table.Column<int>(type: "integer", nullable: true),
                    DispatchAttempts = table.Column<int>(type: "integer", nullable: false),
                    LastPolledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    NextPollAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ProcessingDeadlineAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LeaseOwner = table.Column<string>(type: "text", nullable: true),
                    LeaseExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastError = table.Column<string>(type: "text", nullable: true),
                    Revision = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Judge0Executions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Judge0Executions_ExecutionJobs_ExecutionJobId",
                        column: x => x.ExecutionJobId,
                        principalTable: "ExecutionJobs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Judge0Executions_ExecutionJobId",
                table: "Judge0Executions",
                column: "ExecutionJobId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Judge0Executions_NextPollAt_LeaseExpiresAt",
                table: "Judge0Executions",
                columns: new[] { "NextPollAt", "LeaseExpiresAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Judge0Executions");
        }
    }
}
