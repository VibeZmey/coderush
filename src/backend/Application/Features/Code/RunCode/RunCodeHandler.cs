using Application.Common.Interfaces;
using Domain.Models.Execution;
using Domain.Models.Matches;

namespace Application.Features.Code.RunCode;

public sealed class RunCodeHandler(ICodeRunRepository runs) : IHandler<RunCodeCommand, RunCodeResult>
{
    public async Task<RunCodeResult> HandleAsync(RunCodeCommand request)
    {
        var problem = await runs.GetMatchProblemAsync(request.MatchProblemId);
        var duel = problem?.Match as DuelMatch;
        if (problem is null || !(duel?.HasPlayer(request.UserId) ?? (problem.Match.UserId == request.UserId)))
            return new(RunCodeOutcome.NotFound);

        var now = DateTimeOffset.UtcNow;
        if (problem.Match.Status != MatchStatus.InProgress || problem.Match.FinishedAt is not null ||
            problem.Status != MatchProblemStatus.Active || problem.ClosedAt is not null ||
            problem.StartedAt is null || problem.StartedAt > now ||
            !problem.IsSubmissionOpen(now))
            return new(RunCodeOutcome.InvalidState);

        var language = problem.ProblemVersion.Languages.SingleOrDefault(
            item => item.ProgrammingLanguageId == request.ProgrammingLanguageId);
        if (language is null || !language.ProgrammingLanguage.IsEnabled)
            return new(RunCodeOutcome.LanguageUnavailable);

        var job = new ExecutionJob
        {
            ProgrammingLanguageId = request.ProgrammingLanguageId,
            SourceCode = request.SourceCode,
            StandardInput = request.StandardInput,
            CpuTimeLimitSeconds = problem.ProblemVersion.CpuTimeLimitSeconds,
            WallTimeLimitSeconds = problem.ProblemVersion.WallTimeLimitSeconds,
            MemoryLimitKilobytes = problem.ProblemVersion.MemoryLimitKilobytes,
            OutputLimitKilobytes = problem.ProblemVersion.OutputLimitKilobytes,
            Status = ProcessingStatus.Pending
        };
        var run = new CodeRun
        {
            MatchProblemId = problem.Id,
            DuelPlayerId = duel is null ? null : request.UserId,
            ExecutionJobId = job.Id,
            ExecutionJob = job
        };

        await runs.AddAsync(run);
        return new(RunCodeOutcome.Accepted, new AcceptedCodeRun(run.Id, job.Status, run.CreatedAt));
    }
}

