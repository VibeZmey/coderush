using Infrastructure.Judge0.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class Judge0ExecutionConfiguration : IEntityTypeConfiguration<Judge0Execution>
{
    public void Configure(EntityTypeBuilder<Judge0Execution> builder)
    {
        builder.ToTable("Judge0Executions");
        builder.HasOne(x => x.ExecutionJob).WithOne().HasForeignKey<Judge0Execution>(x => x.ExecutionJobId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Property(x => x.Revision).IsConcurrencyToken();
        builder.HasIndex(x => new { x.NextPollAt, x.LeaseExpiresAt });
    }
}
