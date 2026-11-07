using Domain.Models.Execution;
using Domain.Models.Matches;
using Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class DuelDraftConfiguration : IEntityTypeConfiguration<DuelDraft>
{
    public void Configure(EntityTypeBuilder<DuelDraft> builder)
    {
        builder.ToTable("DuelDrafts");
        builder.HasKey(x => new { x.MatchId, x.UserId });
        builder.HasOne<DuelMatch>().WithMany().HasForeignKey(x => x.MatchId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ProgrammingLanguage).WithMany().HasForeignKey(x => x.ProgrammingLanguageId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
