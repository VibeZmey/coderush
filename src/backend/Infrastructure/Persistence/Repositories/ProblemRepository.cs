using Application.Common.Interfaces;
using Domain.Models.Problems;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Infrastructure.Persistence.Repositories;

public sealed class ProblemRepository(AppDbContext context) : IProblemRepository
{
    public async Task<IReadOnlyList<int>> GetEnabledLanguageIdsAsync(IReadOnlyCollection<int> ids) =>
        await context.ProgrammingLanguages.AsNoTracking()
            .Where(language => language.IsEnabled && ids.Contains(language.Id))
            .Select(language => language.Id).ToArrayAsync();

    public async Task<bool> TryAddAsync(Problem problem)
    {
        context.Problems.Add(problem);
        try
        {
            await context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateException error) when (error.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "IX_Problems_Key" })
        {
            return false;
        }
    }
}
