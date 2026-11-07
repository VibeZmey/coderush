using Domain.Models.Problems;
using Infrastructure.Persistence;
using Infrastructure.Persistence.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Tests.Problems;

public sealed class Judge0LanguageCatalogTests
{
    [Fact]
    public void SeedsAllPublicCatalogIdsAndEnablesSelectedFamilies()
    {
        var insert = new UseJudge0LanguageIds().UpOperations.OfType<InsertDataOperation>().Single();
        Assert.Equal(71, insert.Values.GetLength(0));
        var rows = Enumerable.Range(0, insert.Values.GetLength(0)).ToArray();
        Assert.Equal(Enumerable.Range(43, 71), rows.Select(row => (int)insert.Values[row, 0]!));
        Assert.Equal("Python (3.8.1)", insert.Values[71 - 43, 2]);
        Assert.Equal("C# (Mono 6.6.0.161)", insert.Values[51 - 43, 2]);
        Assert.True((bool)insert.Values[71 - 43, 4]!);
        Assert.True((bool)insert.Values[51 - 43, 4]!);
        Assert.False((bool)insert.Values[62 - 43, 4]!);
    }

    [Fact]
    public void CatalogUsesExplicitIntIdsWhileProblemLanguageKeepsItsOwnGuid()
    {
        using var context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=unused;Username=unused;Password=unused").Options);
        var catalogId = context.Model.FindEntityType(typeof(ProgrammingLanguage))!.FindProperty("Id")!;
        var settings = context.Model.FindEntityType(typeof(ProblemLanguage))!;

        Assert.Equal(typeof(int), catalogId.ClrType);
        Assert.Equal(ValueGenerated.Never, catalogId.ValueGenerated);
        Assert.Equal(typeof(Guid), settings.FindProperty("Id")!.ClrType);
        Assert.Equal(typeof(int), settings.FindProperty("ProgrammingLanguageId")!.ClrType);
    }
}
