using Domain.Models.Execution;

namespace Application.Features.Code.RunCode;

public sealed record CodeRunView(Guid Id, ProcessingStatus Status, DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt, string? FailureCode, ExecutionResult? Result);

public interface ICodeRunReader
{
    Task<CodeRunView?> GetAsync(Guid id, string userId, CancellationToken cancellationToken);
}
