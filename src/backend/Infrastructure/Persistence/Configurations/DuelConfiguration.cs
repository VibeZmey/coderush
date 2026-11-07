using Domain.Models.Matches;
using Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class DuelMatchConfiguration : IEntityTypeConfiguration<DuelMatch>
{
    public void Configure(EntityTypeBuilder<DuelMatch> builder)
    {
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.OpponentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.OpponentId);
    }
}

public sealed class DuelQueueConfiguration : IEntityTypeConfiguration<DuelQueueEntry>
{
    public void Configure(EntityTypeBuilder<DuelQueueEntry> builder)
    {
        builder.ToTable("DuelQueue");
        builder.HasKey(x => x.UserId);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
