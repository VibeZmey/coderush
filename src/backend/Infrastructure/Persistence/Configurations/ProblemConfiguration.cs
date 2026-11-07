using Domain.Models.Problems;
using Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class ProblemConfiguration : IEntityTypeConfiguration<Problem>
{
    public void Configure(EntityTypeBuilder<Problem> builder)
    {
        builder.ToTable("Problems");
        builder.Property(problem => problem.Key).HasMaxLength(128).IsRequired();
        builder.HasIndex(problem => problem.Key).IsUnique();
        builder.HasOne<User>().WithMany().HasForeignKey(problem => problem.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
