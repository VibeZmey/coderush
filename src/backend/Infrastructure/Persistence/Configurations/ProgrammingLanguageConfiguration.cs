using Domain.Models.Problems;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Infrastructure.Persistence.Seeding;

namespace Infrastructure.Persistence.Configurations;

public sealed class ProgrammingLanguageConfiguration : IEntityTypeConfiguration<ProgrammingLanguage>
{
    public void Configure(EntityTypeBuilder<ProgrammingLanguage> builder)
    {
        builder.ToTable("ProgrammingLanguages");
        builder.Property(language => language.Id).ValueGeneratedNever();
        builder.Property(language => language.Key).HasMaxLength(128).IsRequired();
        builder.Property(language => language.DisplayName).HasMaxLength(256).IsRequired();
        builder.Property(language => language.FileExtension).HasMaxLength(32).IsRequired();
        builder.Property(language => language.RuntimeVersion).HasMaxLength(128).IsRequired();
        builder.HasIndex(language => language.Key).IsUnique();
        builder.HasData(Judge0LanguageCatalog.Languages);
    }
}
