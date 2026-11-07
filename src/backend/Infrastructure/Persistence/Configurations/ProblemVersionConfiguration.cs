using Domain.Models.Problems;
using Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class ProblemVersionConfiguration : IEntityTypeConfiguration<ProblemVersion>
{
    public void Configure(EntityTypeBuilder<ProblemVersion> builder)
    {
        builder.ToTable("ProblemVersions", table => table.HasCheckConstraint(
            "CK_ProblemVersions_PositiveLimits",
            "\"CpuTimeLimitSeconds\" > 0 AND \"WallTimeLimitSeconds\" > 0 AND \"MemoryLimitKilobytes\" > 0 AND \"OutputLimitKilobytes\" > 0"));
        builder.Property(version => version.Title).HasMaxLength(256).IsRequired();
        builder.Property(version => version.DescriptionMarkdown).IsRequired();
        builder.HasIndex(version => new { version.ProblemId, version.VersionNumber }).IsUnique();
        builder.HasOne(version => version.Problem).WithMany(problem => problem.Versions)
            .HasForeignKey(version => version.ProblemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(version => version.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
