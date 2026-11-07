using Domain.Models.Matches;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class MatchProblemConfiguration : IEntityTypeConfiguration<MatchProblem>
{
    public void Configure(EntityTypeBuilder<MatchProblem> builder)
    {
        builder.ToTable("MatchProblems");
        builder.Property(problem => problem.Id).ValueGeneratedNever();
        builder.HasOne(problem => problem.Match).WithMany(match => match.Problems)
            .HasForeignKey(problem => problem.MatchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(problem => problem.ProblemVersion).WithMany()
            .HasForeignKey(problem => problem.ProblemVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(problem => new { problem.MatchId, problem.Position }).IsUnique();
        builder.HasIndex(problem => new { problem.MatchId, problem.ProblemVersionId }).IsUnique();
        builder.HasOne(problem => problem.FirstAcceptedSubmission).WithMany()
            .HasForeignKey(problem => problem.FirstAcceptedSubmissionId).OnDelete(DeleteBehavior.Restrict);
    }
}
