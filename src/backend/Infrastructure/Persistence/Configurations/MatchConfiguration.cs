using Domain.Models.Matches;
using Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MatchType = Domain.Models.Matches.MatchType;

namespace Infrastructure.Persistence.Configurations;

public sealed class MatchConfiguration : IEntityTypeConfiguration<Match>
{
    public void Configure(EntityTypeBuilder<Match> builder)
    {
        builder.ToTable("Matches", table =>
        {
            table.HasCheckConstraint("CK_Matches_TrainingCount", "\"Type\" <> 0 OR \"Count\" IS NOT NULL AND \"Count\" BETWEEN 1 AND 100");
            table.HasCheckConstraint("CK_Matches_TrainingDifficulty", "\"Type\" <> 0 OR \"Difficulty\" IS NOT NULL AND \"Difficulty\" IN (1, 2, 3)");
            table.HasCheckConstraint("CK_Matches_TrainingTime", "\"Type\" <> 0 OR \"TimePerProblemSeconds\" IS NOT NULL AND \"TimePerProblemSeconds\" BETWEEN 1 AND 86400");
        });
        builder.HasKey(match => match.Id);
        builder.Property(match => match.UserId).IsRequired();
        builder.HasDiscriminator<MatchType>("Type").HasValue<TrainingMatch>(MatchType.Training)
            .HasValue<DuelMatch>(MatchType.Duel).HasValue<PracticeMatch>(MatchType.Practice);
        builder.HasOne<User>().WithMany().HasForeignKey(match => match.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(match => new { match.UserId, match.CreatedAt });

    }
}

