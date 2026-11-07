using Domain.Models.Execution;
using Domain.Models.Matches;

namespace Application.Common.Interfaces;

public interface ICodeRunRepository
{
    Task<MatchProblem?> GetMatchProblemAsync(Guid id);
    Task AddAsync(CodeRun run);
}
