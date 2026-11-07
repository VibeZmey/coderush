using Domain.Models.Execution;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class CodeDraftConfiguration : IEntityTypeConfiguration<CodeDraft>
{
    public void Configure(EntityTypeBuilder<CodeDraft> builder)
    {
        builder.ToTable("CodeDrafts");
        builder.Property(draft => draft.Id).ValueGeneratedNever();
        builder.HasIndex(draft => new { draft.MatchProblemId, draft.ProgrammingLanguageId }).IsUnique();
        builder.HasOne(draft => draft.MatchProblem).WithMany(problem => problem.Drafts)
            .HasForeignKey(draft => draft.MatchProblemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(draft => draft.ProgrammingLanguage).WithMany()
            .HasForeignKey(draft => draft.ProgrammingLanguageId).OnDelete(DeleteBehavior.Restrict);
    }
}
