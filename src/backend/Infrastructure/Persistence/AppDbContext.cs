using Infrastructure.Identity;
using Domain.Models.Matches;
using Domain.Models.Execution;
using Domain.Models.Problems;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public class AppDbContext : IdentityDbContext<User>
{
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<TrainingMatch> TrainingMatches => Set<TrainingMatch>();
    public DbSet<MatchProblem> MatchProblems => Set<MatchProblem>();
    public DbSet<Problem> Problems => Set<Problem>();
    public DbSet<ProblemVersion> ProblemVersions => Set<ProblemVersion>();
    public DbSet<ProblemLanguage> ProblemLanguages => Set<ProblemLanguage>();
    public DbSet<ProblemTestCase> ProblemTestCases => Set<ProblemTestCase>();
    public DbSet<ProgrammingLanguage> ProgrammingLanguages => Set<ProgrammingLanguage>();
    public DbSet<CodeRun> CodeRuns => Set<CodeRun>();
    public DbSet<CodeDraft> CodeDrafts => Set<CodeDraft>();
    public DbSet<ExecutionJob> ExecutionJobs => Set<ExecutionJob>();
    
    
    public AppDbContext(
        DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public IQueryable<string> AdminUserIds() =>
        from membership in UserRoles
        join role in Roles on membership.RoleId equals role.Id
        where role.Name == Application.Common.Constants.UserRoles.Admin
        select membership.UserId;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<User>().HasIndex(x => x.NormalizedEmail).IsUnique();
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
