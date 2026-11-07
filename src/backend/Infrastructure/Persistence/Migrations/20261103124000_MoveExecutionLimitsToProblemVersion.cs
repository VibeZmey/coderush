using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    public partial class MoveExecutionLimitsToProblemVersion : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(name: "CpuTimeLimitSeconds", table: "ProblemVersions", type: "double precision", nullable: false, defaultValue: 2d);
            migrationBuilder.AddColumn<double>(name: "WallTimeLimitSeconds", table: "ProblemVersions", type: "double precision", nullable: false, defaultValue: 10d);
            migrationBuilder.AddColumn<int>(name: "MemoryLimitKilobytes", table: "ProblemVersions", type: "integer", nullable: false, defaultValue: 128000);
            migrationBuilder.AddColumn<int>(name: "OutputLimitKilobytes", table: "ProblemVersions", type: "integer", nullable: false, defaultValue: 1024);

            // Keep existing limits before removing language-specific columns.
            // Different language values become the most permissive limit for the version.
            migrationBuilder.Sql("""
                UPDATE "ProblemVersions" AS version
                SET "CpuTimeLimitSeconds" = limits."CpuTimeLimitSeconds",
                    "WallTimeLimitSeconds" = limits."WallTimeLimitSeconds",
                    "MemoryLimitKilobytes" = limits."MemoryLimitKilobytes",
                    "OutputLimitKilobytes" = limits."OutputLimitKilobytes"
                FROM (
                    SELECT "ProblemVersionId",
                           MAX("CpuTimeLimitSeconds") AS "CpuTimeLimitSeconds",
                           MAX("WallTimeLimitSeconds") AS "WallTimeLimitSeconds",
                           MAX("MemoryLimitKilobytes") AS "MemoryLimitKilobytes",
                           MAX("OutputLimitKilobytes") AS "OutputLimitKilobytes"
                    FROM "ProblemLanguages"
                    GROUP BY "ProblemVersionId"
                ) AS limits
                WHERE version."Id" = limits."ProblemVersionId";
                """);

            migrationBuilder.DropCheckConstraint(name: "CK_ProblemLanguages_PositiveLimits", table: "ProblemLanguages");
            migrationBuilder.DropColumn(name: "CpuTimeLimitSeconds", table: "ProblemLanguages");
            migrationBuilder.DropColumn(name: "WallTimeLimitSeconds", table: "ProblemLanguages");
            migrationBuilder.DropColumn(name: "MemoryLimitKilobytes", table: "ProblemLanguages");
            migrationBuilder.DropColumn(name: "OutputLimitKilobytes", table: "ProblemLanguages");
            migrationBuilder.AddCheckConstraint(name: "CK_ProblemVersions_PositiveLimits", table: "ProblemVersions",
                sql: "\"CpuTimeLimitSeconds\" > 0 AND \"WallTimeLimitSeconds\" > 0 AND \"MemoryLimitKilobytes\" > 0 AND \"OutputLimitKilobytes\" > 0");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(name: "CpuTimeLimitSeconds", table: "ProblemLanguages", type: "double precision", nullable: false, defaultValue: 2d);
            migrationBuilder.AddColumn<double>(name: "WallTimeLimitSeconds", table: "ProblemLanguages", type: "double precision", nullable: false, defaultValue: 10d);
            migrationBuilder.AddColumn<int>(name: "MemoryLimitKilobytes", table: "ProblemLanguages", type: "integer", nullable: false, defaultValue: 128000);
            migrationBuilder.AddColumn<int>(name: "OutputLimitKilobytes", table: "ProblemLanguages", type: "integer", nullable: false, defaultValue: 1024);

            // A rollback gives each language the version's current shared limits.
            migrationBuilder.Sql("""
                UPDATE "ProblemLanguages" AS language
                SET "CpuTimeLimitSeconds" = version."CpuTimeLimitSeconds",
                    "WallTimeLimitSeconds" = version."WallTimeLimitSeconds",
                    "MemoryLimitKilobytes" = version."MemoryLimitKilobytes",
                    "OutputLimitKilobytes" = version."OutputLimitKilobytes"
                FROM "ProblemVersions" AS version
                WHERE language."ProblemVersionId" = version."Id";
                """);

            migrationBuilder.DropCheckConstraint(name: "CK_ProblemVersions_PositiveLimits", table: "ProblemVersions");
            migrationBuilder.DropColumn(name: "CpuTimeLimitSeconds", table: "ProblemVersions");
            migrationBuilder.DropColumn(name: "WallTimeLimitSeconds", table: "ProblemVersions");
            migrationBuilder.DropColumn(name: "MemoryLimitKilobytes", table: "ProblemVersions");
            migrationBuilder.DropColumn(name: "OutputLimitKilobytes", table: "ProblemVersions");
            migrationBuilder.AddCheckConstraint(name: "CK_ProblemLanguages_PositiveLimits", table: "ProblemLanguages",
                sql: "\"CpuTimeLimitSeconds\" > 0 AND \"WallTimeLimitSeconds\" > 0 AND \"MemoryLimitKilobytes\" > 0 AND \"OutputLimitKilobytes\" > 0");
        }
    }
}
