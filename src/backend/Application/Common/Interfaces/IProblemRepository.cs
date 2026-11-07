using Domain.Models.Problems;

namespace Application.Common.Interfaces;

public interface IProblemRepository
{
    Task<IReadOnlyList<int>> GetEnabledLanguageIdsAsync(IReadOnlyCollection<int> ids);
    Task<bool> TryAddAsync(Problem problem);
}
