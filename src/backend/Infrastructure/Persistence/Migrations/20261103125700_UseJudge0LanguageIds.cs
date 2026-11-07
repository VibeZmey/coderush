using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Persistence.Migrations
{
    public partial class UseJudge0LanguageIds : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                CREATE TABLE "ProgrammingLanguages_Judge0" (
                    "Id" integer NOT NULL,
                    "CreatedAt" timestamp with time zone NOT NULL,
                    "DisplayName" character varying(256) NOT NULL,
                    "FileExtension" character varying(32) NOT NULL,
                    "IsEnabled" boolean NOT NULL,
                    "Key" character varying(128) NOT NULL,
                    "RuntimeVersion" character varying(128) NOT NULL,
                    CONSTRAINT "PK_ProgrammingLanguages_Judge0" PRIMARY KEY ("Id")
                );
                """);
            migrationBuilder.InsertData(
                table: "ProgrammingLanguages_Judge0",
                columnTypes: new[] { "integer", "timestamp with time zone", "character varying(256)", "character varying(32)", "boolean", "character varying(128)", "character varying(128)" },
                columns: new[] { "Id", "CreatedAt", "DisplayName", "FileExtension", "IsEnabled", "Key", "RuntimeVersion" },
                values: new object[,]
                {
                    { 43, new DateTimeOffset(new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Plain Text", ".txt", false, "judge0-43", "" },
                    { 44, new DateTimeOffset(new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Executable", ".bin", false, "judge0-44", "" },
                    { 45, new DateTimeOffset(new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Assembly (NASM 2.14.02)", ".asm", false, "judge0-45", "NASM 2.14.02" },
                    { 46, new DateTimeOffset(new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Bash (5.0.0)", ".sh", false, "judge0-46", "5.0.0" },
                    { 47, new DateTimeOffset(new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Basic (FBC 1.07.1)", ".bas", false, "judge0-47", "FBC 1.07.1" },
                    { 48, new DateTimeOffset(new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "C (GCC 7.4.0)", ".c", true, "judge0-48", "GCC 7.4.0" },
                    { 49, new DateTimeOffset(new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "C (GCC 8.3.0)", ".c", true, "judge0-49", "GCC 8.3.0" },
                    { 50, new DateTimeOffset(new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "C (GCC 9.2.0)", ".c", true, "judge0-50", "GCC 9.2.0" },
                    { 51, new DateTimeOffset(new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "C# (Mono 6.6.0.161)", ".cs", true, "judge0-51", "Mono 6.6.0.161" },
                    { 52, new DateTimeOffset(new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "C++ (GCC 7.4.0)", ".cpp", true, "judge0-52", "GCC 7.4.0" },
                    { 53, new DateTimeOffset(new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "C++ (GCC 8.3.0)", ".cpp", true, "judge0-53", "GCC 8.3.0" },
                    { 54, new DateTimeOffset(new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "C++ (GCC 9.2.0)", ".cpp", true, "judge0-54", "GCC 9.2.0" },
                    { 55, new DateTimeOffset(new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Common Lisp (SBCL 2.0.0)", ".lisp", false, "judge0-55", "SBCL 2.0.0" },
                    { 56, new DateTimeOffset(new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "D (DMD 2.089.1)", ".d", false, "judge0-56", "DMD 2.089.1" },
                    { 57, new DateTimeOffset(new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Elixir (1.9.4)", ".exs", false, "judge0-57", "1.9.4" },
                    { 58, new DateTimeOffset(new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Erlang (OTP 22.2)", ".erl", false, "judge0-58", "OTP 22.2" },
                    { 59, new DateTimeOffset(new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Fortran (GFortran 9.2.0)", ".f90", false, "judge0-59", "GFortran 9.2.0" },
                    { 60, new DateTimeOffset(new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Go (1.13.5)", ".go", true, "judge0-60", "1.13.5" },
                    { 61, new DateTimeOffset(new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Haskell (GHC 8.8.1)", ".hs", false, "judge0-61", "GHC 8.8.1" },
                    { 62, new DateTimeOffset(new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Java (OpenJDK 13.0.1)", ".java", false, "judge0-62", "OpenJDK 13.0.1" },
                    { 63, new DateTimeOffset(new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "JavaScript (Node.js 12.14.0)", ".js", true, "judge0-63", "Node.js 12.14.0" },
                    { 64, new DateTimeOffset(new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Lua (5.3.5)", ".lua", false, "judge0-64", "5.3.5" },
                    { 65, new DateTimeOffset(new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "OCaml (4.09.0)", ".ml", false, "judge0-65", "4.09.0" },
                    { 66, new DateTimeOffset(new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Octave (5.1.0)", ".m", false, "judge0-66", "5.1.0" },
                    { 67, new DateTimeOffset(new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Pascal (FPC 3.0.4)", ".pas", false, "judge0-67", "FPC 3.0.4" },
                    { 68, new DateTimeOffset(new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "PHP (7.4.1)", ".php", false, "judge0-68", "7.4.1" },
                    { 69, new DateTimeOffset(new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Prolog (GNU Prolog 1.4.5)", ".pro", false, "judge0-69", "GNU Prolog 1.4.5" },
                    { 70, new DateTimeOffset(new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Python (2.7.17)", ".py", true, "judge0-70", "2.7.17" },
                    { 71, new DateTimeOffset(new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Python (3.8.1)", ".py", true, "judge0-71", "3.8.1" },
                    { 72, new DateTimeOffset(new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Ruby (2.7.0)", ".rb", false, "judge0-72", "2.7.0" },
                    { 73, new DateTimeOffset(new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Rust (1.40.0)", ".rs", false, "judge0-73", "1.40.0" },
                    { 74, new DateTimeOffset(new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "TypeScript (3.7.4)", ".ts", false, "judge0-74", "3.7.4" },
                    { 75, new DateTimeOffset(new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "C (Clang 7.0.1)", ".c", true, "judge0-75", "Clang 7.0.1" },
                    { 76, new DateTimeOffset(new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "C++ (Clang 7.0.1)", ".cpp", true, "judge0-76", "Clang 7.0.1" },
                    { 77, new DateTimeOffset(new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "COBOL (GnuCOBOL 2.2)", ".cob", false, "judge0-77", "GnuCOBOL 2.2" },
                    { 78, new DateTimeOffset(new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Kotlin (1.3.70)", ".kt", false, "judge0-78", "1.3.70" },
                    { 79, new DateTimeOffset(new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Objective-C (Clang 7.0.1)", ".m", false, "judge0-79", "Clang 7.0.1" },
                    { 80, new DateTimeOffset(new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "R (4.0.0)", ".r", false, "judge0-80", "4.0.0" },
                    { 81, new DateTimeOffset(new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Scala (2.13.2)", ".scala", false, "judge0-81", "2.13.2" },
                    { 82, new DateTimeOffset(new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "SQL (SQLite 3.27.2)", ".sql", false, "judge0-82", "SQLite 3.27.2" },
                    { 83, new DateTimeOffset(new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Swift (5.2.3)", ".swift", false, "judge0-83", "5.2.3" },
                    { 84, new DateTimeOffset(new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Visual Basic.Net (vbnc 0.0.0.5943)", ".vb", false, "judge0-84", "vbnc 0.0.0.5943" },
                    { 85, new DateTimeOffset(new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Perl (5.28.1)", ".pl", false, "judge0-85", "5.28.1" },
                    { 86, new DateTimeOffset(new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Clojure (1.10.1)", ".clj", false, "judge0-86", "1.10.1" },
                    { 87, new DateTimeOffset(new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "F# (.NET Core SDK 3.1.202)", ".fs", false, "judge0-87", ".NET Core SDK 3.1.202" },
                    { 88, new DateTimeOffset(new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Groovy (3.0.3)", ".groovy", false, "judge0-88", "3.0.3" },
                    { 89, new DateTimeOffset(new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Multi-file program", ".zip", false, "judge0-89", "" },
                    { 90, new DateTimeOffset(new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Dart (2.19.2)", ".dart", false, "judge0-90", "2.19.2" },
                    { 91, new DateTimeOffset(new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Java (JDK 17.0.6)", ".java", false, "judge0-91", "JDK 17.0.6" },
                    { 92, new DateTimeOffset(new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Python (3.11.2)", ".py", true, "judge0-92", "3.11.2" },
                    { 93, new DateTimeOffset(new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "JavaScript (Node.js 18.15.0)", ".js", true, "judge0-93", "Node.js 18.15.0" },
                    { 94, new DateTimeOffset(new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "TypeScript (5.0.3)", ".ts", false, "judge0-94", "5.0.3" },
                    { 95, new DateTimeOffset(new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Go (1.18.5)", ".go", true, "judge0-95", "1.18.5" },
                    { 96, new DateTimeOffset(new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "JavaFX (JDK 17.0.6, OpenJFX 22.0.2)", ".java", false, "judge0-96", "JDK 17.0.6, OpenJFX 22.0.2" },
                    { 97, new DateTimeOffset(new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "JavaScript (Node.js 20.17.0)", ".js", true, "judge0-97", "Node.js 20.17.0" },
                    { 98, new DateTimeOffset(new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "PHP (8.3.11)", ".php", false, "judge0-98", "8.3.11" },
                    { 99, new DateTimeOffset(new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "R (4.4.1)", ".r", false, "judge0-99", "4.4.1" },
                    { 100, new DateTimeOffset(new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Python (3.12.5)", ".py", true, "judge0-100", "3.12.5" },
                    { 101, new DateTimeOffset(new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "TypeScript (5.6.2)", ".ts", false, "judge0-101", "5.6.2" },
                    { 102, new DateTimeOffset(new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "JavaScript (Node.js 22.08.0)", ".js", true, "judge0-102", "Node.js 22.08.0" },
                    { 103, new DateTimeOffset(new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "C (GCC 14.1.0)", ".c", true, "judge0-103", "GCC 14.1.0" },
                    { 104, new DateTimeOffset(new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "C (Clang 18.1.8)", ".c", true, "judge0-104", "Clang 18.1.8" },
                    { 105, new DateTimeOffset(new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "C++ (GCC 14.1.0)", ".cpp", true, "judge0-105", "GCC 14.1.0" },
                    { 106, new DateTimeOffset(new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Go (1.22.0)", ".go", true, "judge0-106", "1.22.0" },
                    { 107, new DateTimeOffset(new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Go (1.23.5)", ".go", true, "judge0-107", "1.23.5" },
                    { 108, new DateTimeOffset(new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Rust (1.85.0)", ".rs", false, "judge0-108", "1.85.0" },
                    { 109, new DateTimeOffset(new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Python (3.13.2)", ".py", true, "judge0-109", "3.13.2" },
                    { 110, new DateTimeOffset(new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "C (Clang 19.1.7)", ".c", true, "judge0-110", "Clang 19.1.7" },
                    { 111, new DateTimeOffset(new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Kotlin (2.1.10)", ".kt", false, "judge0-111", "2.1.10" },
                    { 112, new DateTimeOffset(new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Scala (3.4.2)", ".scala", false, "judge0-112", "3.4.2" },
                    { 113, new DateTimeOffset(new DateTime(2026, 11, 3, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "Python (3.14.0)", ".py", true, "judge0-113", "3.14.0" }
                });

            // Resolve legacy GUIDs once during migration; no mapping table remains afterward.
            migrationBuilder.Sql("""
                ALTER TABLE "ProgrammingLanguages" ADD COLUMN "_Judge0Id" integer;
                UPDATE "ProgrammingLanguages" AS old
                SET "_Judge0Id" = (
                    SELECT catalog."Id" FROM "ProgrammingLanguages_Judge0" AS catalog
                    WHERE old."Key" = catalog."Key"
                       OR old."DisplayName" = catalog."DisplayName"
                       OR catalog."DisplayName" = old."DisplayName" || ' (' || old."RuntimeVersion" || ')'
                    ORDER BY CASE WHEN old."Key" = catalog."Key" THEN 0
                                  WHEN old."DisplayName" = catalog."DisplayName" THEN 1 ELSE 2 END
                    LIMIT 1
                );
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM "ProgrammingLanguages" WHERE "_Judge0Id" IS NULL) THEN
                        RAISE EXCEPTION 'Cannot convert existing languages to Judge0 IDs. Match their Key or DisplayName/RuntimeVersion to the Judge0 catalog before retrying.';
                    END IF;
                END $$;

                UPDATE "ProgrammingLanguages_Judge0" AS catalog
                SET "IsEnabled" = previous.enabled, "CreatedAt" = previous.created
                FROM (SELECT "_Judge0Id", bool_or("IsEnabled") AS enabled, min("CreatedAt") AS created
                      FROM "ProgrammingLanguages" GROUP BY "_Judge0Id") AS previous
                WHERE catalog."Id" = previous."_Judge0Id";

                ALTER TABLE "ProblemLanguages" ADD COLUMN "_Judge0Id" integer;
                UPDATE "ProblemLanguages" AS settings SET "_Judge0Id" = language."_Judge0Id"
                FROM "ProgrammingLanguages" AS language WHERE settings."ProgrammingLanguageId" = language."Id";
                ALTER TABLE "ProblemLanguages" ALTER COLUMN "_Judge0Id" SET NOT NULL;

                ALTER TABLE "ExecutionJobs" ADD COLUMN "_Judge0Id" integer;
                UPDATE "ExecutionJobs" AS job SET "_Judge0Id" = language."_Judge0Id"
                FROM "ProgrammingLanguages" AS language WHERE job."ProgrammingLanguageId" = language."Id";
                ALTER TABLE "ExecutionJobs" ALTER COLUMN "_Judge0Id" SET NOT NULL;

                ALTER TABLE "ProblemLanguages" DROP CONSTRAINT "FK_ProblemLanguages_ProgrammingLanguages_ProgrammingLanguageId";
                ALTER TABLE "ExecutionJobs" DROP CONSTRAINT "FK_ExecutionJobs_ProgrammingLanguages_ProgrammingLanguageId";
                ALTER TABLE "ProblemLanguages" DROP COLUMN "ProgrammingLanguageId";
                ALTER TABLE "ProblemLanguages" RENAME COLUMN "_Judge0Id" TO "ProgrammingLanguageId";
                ALTER TABLE "ExecutionJobs" DROP COLUMN "ProgrammingLanguageId";
                ALTER TABLE "ExecutionJobs" RENAME COLUMN "_Judge0Id" TO "ProgrammingLanguageId";

                DROP TABLE "ProgrammingLanguages";
                ALTER TABLE "ProgrammingLanguages_Judge0" RENAME TO "ProgrammingLanguages";
                ALTER TABLE "ProgrammingLanguages" RENAME CONSTRAINT "PK_ProgrammingLanguages_Judge0" TO "PK_ProgrammingLanguages";
                CREATE UNIQUE INDEX "IX_ProgrammingLanguages_Key" ON "ProgrammingLanguages" ("Key");
                CREATE UNIQUE INDEX "IX_ProblemLanguages_ProblemVersionId_ProgrammingLanguageId" ON "ProblemLanguages" ("ProblemVersionId", "ProgrammingLanguageId");
                CREATE INDEX "IX_ProblemLanguages_ProgrammingLanguageId" ON "ProblemLanguages" ("ProgrammingLanguageId");
                CREATE INDEX "IX_ExecutionJobs_ProgrammingLanguageId" ON "ExecutionJobs" ("ProgrammingLanguageId");
                ALTER TABLE "ProblemLanguages" ADD CONSTRAINT "FK_ProblemLanguages_ProgrammingLanguages_ProgrammingLanguageId"
                    FOREIGN KEY ("ProgrammingLanguageId") REFERENCES "ProgrammingLanguages" ("Id") ON DELETE RESTRICT;
                ALTER TABLE "ExecutionJobs" ADD CONSTRAINT "FK_ExecutionJobs_ProgrammingLanguages_ProgrammingLanguageId"
                    FOREIGN KEY ("ProgrammingLanguageId") REFERENCES "ProgrammingLanguages" ("Id") ON DELETE RESTRICT;
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Preserve logical language relationships on rollback using deterministic GUIDs.
            // The original pre-migration GUID values require restoring a database backup.
            migrationBuilder.Sql("""
                ALTER TABLE "ProblemLanguages" DROP CONSTRAINT "FK_ProblemLanguages_ProgrammingLanguages_ProgrammingLanguageId";
                ALTER TABLE "ExecutionJobs" DROP CONSTRAINT "FK_ExecutionJobs_ProgrammingLanguages_ProgrammingLanguageId";
                ALTER TABLE "ProblemLanguages" ALTER COLUMN "ProgrammingLanguageId" TYPE uuid
                    USING md5('judge0-language-' || "ProgrammingLanguageId"::text)::uuid;
                ALTER TABLE "ExecutionJobs" ALTER COLUMN "ProgrammingLanguageId" TYPE uuid
                    USING md5('judge0-language-' || "ProgrammingLanguageId"::text)::uuid;
                ALTER TABLE "ProgrammingLanguages" ALTER COLUMN "Id" TYPE uuid
                    USING md5('judge0-language-' || "Id"::text)::uuid;
                ALTER TABLE "ProblemLanguages" ADD CONSTRAINT "FK_ProblemLanguages_ProgrammingLanguages_ProgrammingLanguageId"
                    FOREIGN KEY ("ProgrammingLanguageId") REFERENCES "ProgrammingLanguages" ("Id") ON DELETE RESTRICT;
                ALTER TABLE "ExecutionJobs" ADD CONSTRAINT "FK_ExecutionJobs_ProgrammingLanguages_ProgrammingLanguageId"
                    FOREIGN KEY ("ProgrammingLanguageId") REFERENCES "ProgrammingLanguages" ("Id") ON DELETE RESTRICT;
                """);
        }
    }
}