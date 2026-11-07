using Domain.Models.Execution;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;
using Infrastructure.Judge0.Models;

namespace Tests.Code;

public sealed class CodeRunRepositoryTests
{
    [Fact]
    public async Task StagesRunAndJobTogetherInOneSaveWithoutReinsertingExistingTaskOrLanguage()
    {
        var probe = new SaveProbe();
        await using var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=unused;Username=unused;Password=unused")
            .AddInterceptors(probe).Options);
        var job = new ExecutionJob { ProgrammingLanguageId = 71, SourceCode = "print(1)", StandardInput = "" };
        var run = new CodeRun { MatchProblemId = Guid.NewGuid(), ExecutionJobId = job.Id, ExecutionJob = job };

        await new CodeRunRepository(context).AddAsync(run);

        Assert.Equal(1, probe.Calls);
        Assert.Equal(3, probe.AddedEntities.Length);
        Assert.Contains(run, probe.AddedEntities);
        Assert.Contains(job, probe.AddedEntities);
        Assert.Contains(probe.AddedEntities, entity => entity is Judge0Execution execution && execution.ExecutionJobId == job.Id);
    }

    // Suppress actual I/O: this tests graph staging and SaveChanges usage, not a PostgreSQL round trip.
    private sealed class SaveProbe : SaveChangesInterceptor
    {
        public int Calls { get; private set; }
        public object[] AddedEntities { get; private set; } = [];

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            Calls++;
            AddedEntities = eventData.Context!.ChangeTracker.Entries()
                .Where(entry => entry.State == EntityState.Added).Select(entry => entry.Entity).ToArray();
            return ValueTask.FromResult(InterceptionResult<int>.SuppressWithResult(AddedEntities.Length));
        }
    }
}
