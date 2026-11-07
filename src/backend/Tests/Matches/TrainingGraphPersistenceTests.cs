using Domain.Models.Execution;
using Domain.Models.Matches;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace Tests.Matches;

public sealed class TrainingGraphPersistenceTests
{
    [Fact]
    public async Task NewAssignedTaskWithPreallocatedGuidIsInserted()
    {
        var probe = new SaveProbe();
        await using var context = Context(probe);
        var match = new TrainingMatch { UserId = "player", Count = 1 };
        context.Attach(match);
        var problem = new MatchProblem { MatchId = match.Id, ProblemVersionId = Guid.NewGuid(), Position = 1 };
        match.Problems.Add(problem);
        match.Start();
        await new MatchRepository(context).UpdateAsync(match);

        Assert.Same(problem, Assert.Single(probe.Added));
    }

    [Fact]
    public async Task NewDraftWithPreallocatedGuidIsInsertedInsteadOfUpdated()
    {
        var probe = new SaveProbe();
        await using var context = Context(probe);
        var match = new TrainingMatch { UserId = "player", Count = 1 };
        var problem = new MatchProblem { MatchId = match.Id, ProblemVersionId = Guid.NewGuid(), Position = 1 };
        match.Problems.Add(problem);
        context.Attach(match);
        var draft = new CodeDraft { MatchProblemId = problem.Id, ProgrammingLanguageId = 71, SourceCode = "source" };
        problem.Drafts.Add(draft);
        await new MatchRepository(context).UpdateAsync(match);

        Assert.Same(draft, Assert.Single(probe.Added));
    }

    private static AppDbContext Context(SaveProbe probe) => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseNpgsql("Host=localhost;Database=unused;Username=unused;Password=unused").AddInterceptors(probe).Options);

    private sealed class SaveProbe : SaveChangesInterceptor
    {
        public object[] Added { get; private set; } = [];
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData data,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            Added = data.Context!.ChangeTracker.Entries().Where(entry => entry.State == EntityState.Added)
                .Select(entry => entry.Entity).ToArray();
            return ValueTask.FromResult(InterceptionResult<int>.SuppressWithResult(Added.Length));
        }
    }
}
