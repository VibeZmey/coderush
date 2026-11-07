using Domain.Models.Matches;
using Domain.Models.Problems;

namespace Application.Common.Interfaces;

public interface IMatchRepository
{
    Task<Match?> GetByIdAsync(Guid id);
    Task AddAsync(Match match);
    Task UpdateAsync(Match match);
    Task<IReadOnlyList<Guid>> SelectProblemVersionsAsync(ProblemDifficulty difficulty, int count);
    Task<IReadOnlyList<Match>> ListByUserAsync(string userId);
}
