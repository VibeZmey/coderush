using Domain.Models.Execution;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class SubmissionConfiguration : IEntityTypeConfiguration<Submission>
{
    public void Configure(EntityTypeBuilder<Submission> builder)
    {
        builder.ToTable("Submissions");
        builder.HasOne(x => x.MatchProblem).WithMany(x => x.Submissions).HasForeignKey(x => x.MatchProblemId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ProgrammingLanguage).WithMany().HasForeignKey(x => x.ProgrammingLanguageId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.Status, x.SubmittedAt });
        builder.HasIndex(x => x.MatchProblemId).IsUnique().HasFilter("\"Status\" IN (0, 1, 2) AND \"DuelPlayerId\" IS NULL");
        builder.HasIndex(x => new { x.MatchProblemId, x.DuelPlayerId }).IsUnique()
            .HasFilter("\"Status\" IN (0, 1, 2) AND \"DuelPlayerId\" IS NOT NULL");
    }
}

public sealed class SubmissionTestResultConfiguration : IEntityTypeConfiguration<SubmissionTestResult>
{
    public void Configure(EntityTypeBuilder<SubmissionTestResult> builder)
    {
        builder.ToTable("SubmissionTestResults");
        builder.HasOne(x => x.Submission).WithMany(x => x.TestResults).HasForeignKey(x => x.SubmissionId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.ProblemTestCase).WithMany().HasForeignKey(x => x.ProblemTestCaseId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ExecutionJob).WithOne().HasForeignKey<SubmissionTestResult>(x => x.ExecutionJobId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.SubmissionId, x.ProblemTestCaseId }).IsUnique();
    }
}

