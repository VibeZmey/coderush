using Domain.Models.Execution;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class ExecutionJobConfiguration : IEntityTypeConfiguration<ExecutionJob>
{
    public void Configure(EntityTypeBuilder<ExecutionJob> builder)
    {
        builder.ToTable("ExecutionJobs");
        builder.Property(job => job.SourceCode).IsRequired();
        builder.Property(job => job.StandardInput).IsRequired();
        builder.HasOne(job => job.ProgrammingLanguage).WithMany()
            .HasForeignKey(job => job.ProgrammingLanguageId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(job => job.RetryOfJob).WithMany()
            .HasForeignKey(job => job.RetryOfJobId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(job => new { job.Status, job.CreatedAt });
        builder.OwnsOne(job => job.Result);
    }
}
