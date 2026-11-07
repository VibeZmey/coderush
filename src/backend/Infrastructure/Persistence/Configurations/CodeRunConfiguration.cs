using Domain.Models.Execution;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class CodeRunConfiguration : IEntityTypeConfiguration<CodeRun>
{
    public void Configure(EntityTypeBuilder<CodeRun> builder)
    {
        builder.ToTable("CodeRuns");
        builder.HasOne(run => run.MatchProblem).WithMany(problem => problem.CodeRuns)
            .HasForeignKey(run => run.MatchProblemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(run => run.ExecutionJob).WithOne()
            .HasForeignKey<CodeRun>(run => run.ExecutionJobId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(run => new { run.MatchProblemId, run.CreatedAt });
    }
}
