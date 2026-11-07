using Domain.Models.Matches;
using Domain.Models.Execution;
using Domain.Models.Problems;
using Infrastructure.Identity;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace Tests.Matches;

public sealed class MatchPersistenceModelTests
{
    [Fact]
    public void MapsTrainingCodeRunsAndSubmissions()
    {
        using var context = CreateContext();
        var training = context.Model.FindEntityType(typeof(TrainingMatch))!;
        var match = context.Model.FindEntityType(typeof(Match))!;

        Assert.Equal("Matches", training.GetTableName());
        Assert.Equal(0, (int)training.GetDiscriminatorValue()!);
        Assert.Contains(match.GetForeignKeys(), key =>
            key.PrincipalEntityType.ClrType == typeof(User) && key.DeleteBehavior == DeleteBehavior.Restrict);
        Assert.NotNull(context.Model.FindEntityType(typeof(MatchProblem)));
        Assert.NotNull(context.Model.FindEntityType(typeof(Problem)));
        Assert.NotNull(context.Model.FindEntityType(typeof(CodeRun)));
        Assert.NotNull(context.Model.FindEntityType(typeof(ExecutionJob)));
        Assert.NotNull(context.Model.FindEntityType(typeof(Submission)));
        Assert.NotNull(context.Model.FindEntityType(typeof(SubmissionTestResult)));
    }

    [Fact]
    public void MigrationGeneratesPostgresSchemaWithoutConnectingToDatabase()
    {
        using var context = CreateContext();
        var sql = context.GetService<IMigrator>().GenerateScript();

        Assert.Contains("CREATE TABLE \"Matches\"", sql);
        Assert.Contains("REFERENCES \"AspNetUsers\"", sql);
        Assert.Contains("CK_Matches_TrainingCount", sql);
        Assert.Contains("CK_Matches_TrainingDifficulty", sql);
        Assert.Contains("CK_Matches_TrainingTime", sql);
        Assert.Contains("CREATE TABLE \"CodeRuns\"", sql);
        Assert.Contains("CREATE TABLE \"ExecutionJobs\"", sql);
        Assert.Contains("CREATE TABLE \"MatchProblems\"", sql);
        Assert.Contains("INSERT INTO \"AspNetRoles\"", sql);
        Assert.Contains("'Admin'", sql);
        Assert.Contains("CREATE TABLE \"DuelQueue\"", sql);
        Assert.Contains("IX_Submissions_MatchProblemId_DuelPlayerId", sql);
    }

    private static AppDbContext CreateContext() => new(
        new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=unused;Username=unused;Password=unused")
            .Options);

    [Fact]
    public void LimitsMigrationCopiesDataBeforeDroppingOldColumns()
    {
        using var context = CreateContext();
        var sql = context.GetService<IMigrator>().GenerateScript("AddAdminRole", "MoveExecutionLimitsToProblemVersion");

        Assert.Contains("MAX(\"MemoryLimitKilobytes\")", sql);
        Assert.True(sql.IndexOf("UPDATE \"ProblemVersions\"", StringComparison.Ordinal) <
                    sql.IndexOf("DROP COLUMN", StringComparison.Ordinal));
        Assert.Contains("CK_ProblemVersions_PositiveLimits", sql);
        Assert.Null(context.Model.FindEntityType(typeof(ProblemLanguage))!.FindProperty("MemoryLimitKilobytes"));
        Assert.NotNull(context.Model.FindEntityType(typeof(ProblemVersion))!.FindProperty("MemoryLimitKilobytes"));
    }
}
