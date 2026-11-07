using Domain.Models.Problems;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public sealed class ProblemLanguageConfiguration : IEntityTypeConfiguration<ProblemLanguage>
{
    public void Configure(EntityTypeBuilder<ProblemLanguage> builder)
    {
        builder.ToTable("ProblemLanguages");
        builder.Property(language => language.Template).IsRequired();
        builder.HasIndex(language => new { language.ProblemVersionId, language.ProgrammingLanguageId }).IsUnique();
        builder.HasOne(language => language.ProblemVersion).WithMany(version => version.Languages)
            .HasForeignKey(language => language.ProblemVersionId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(language => language.ProgrammingLanguage).WithMany()
            .HasForeignKey(language => language.ProgrammingLanguageId).OnDelete(DeleteBehavior.Restrict);
    }
}
