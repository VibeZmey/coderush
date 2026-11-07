using Domain.Models.Problems;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class ProblemTestCaseConfiguration : IEntityTypeConfiguration<ProblemTestCase>
{
    public void Configure(EntityTypeBuilder<ProblemTestCase> builder)
    {
        builder.ToTable("ProblemTestCases");
        builder.Property(test => test.Input).IsRequired();
        builder.Property(test => test.ExpectedOutput).IsRequired();
        builder.HasIndex(test => new { test.ProblemVersionId, test.Position }).IsUnique();
        builder.HasOne(test => test.ProblemVersion).WithMany(version => version.TestCases)
            .HasForeignKey(test => test.ProblemVersionId).OnDelete(DeleteBehavior.Restrict);
    }
}
