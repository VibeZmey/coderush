using Application.Common.Interfaces;
using Domain.Models.Execution;
using Domain.Models.Matches;

namespace Application.Features.Matches.TrainingSession;

public sealed record GetTrainingMatchQuery(string UserId, Guid MatchId);
public sealed record ListTrainingMatchesQuery(string UserId);
public sealed record NextTrainingProblemCommand(string UserId, Guid MatchId);
public sealed record LeaveTrainingMatchCommand(string UserId, Guid MatchId);
public sealed record SaveTrainingDraftCommand(string UserId, Guid MatchId, Guid MatchProblemId,
    int ProgrammingLanguageId, string SourceCode, string StandardInput);
public enum TrainingSessionOutcome { Success, NotFound, InvalidState, LanguageUnavailable }
public sealed record TrainingSessionResult(TrainingSessionOutcome Outcome, TrainingMatchView? Match = null);

internal static class TrainingSessions
{
    public static async Task<TrainingMatch?> LoadAsync(IMatchRepository repository, string userId, Guid id, DateTimeOffset now)
    {
        if (await repository.GetByIdAsync(id) is not TrainingMatch match || match.UserId != userId)
            return null;
        // Stored deadlines remain authoritative after reloads or an application restart.
        if (match.Refresh(now))
            await repository.UpdateAsync(match);
        return match;
    }
}

public sealed class GetTrainingMatchHandler(IMatchRepository matches) : IHandler<GetTrainingMatchQuery, TrainingSessionResult>
{
    public async Task<TrainingSessionResult> HandleAsync(GetTrainingMatchQuery request)
    {
        var now = DateTimeOffset.UtcNow;
        var match = await TrainingSessions.LoadAsync(matches, request.UserId, request.MatchId, now);
        return match is null ? new(TrainingSessionOutcome.NotFound) :
            new(TrainingSessionOutcome.Success, TrainingMatchViews.Create(match, now));
    }
}

public sealed class ListTrainingMatchesHandler(IMatchRepository matches) : IHandler<ListTrainingMatchesQuery, IReadOnlyList<TrainingMatchView>>
{
    public async Task<IReadOnlyList<TrainingMatchView>> HandleAsync(ListTrainingMatchesQuery request)
    {
        var now = DateTimeOffset.UtcNow;
        var sessions = (await matches.ListByUserAsync(request.UserId)).OfType<TrainingMatch>().ToArray();
        foreach (var match in sessions)
            if (match.Refresh(now))
                await matches.UpdateAsync(match);
        return sessions.Select(match => TrainingMatchViews.Create(match, now)).ToArray();
    }
}

public sealed class NextTrainingProblemHandler(IMatchRepository matches) : IHandler<NextTrainingProblemCommand, TrainingSessionResult>
{
    public async Task<TrainingSessionResult> HandleAsync(NextTrainingProblemCommand request)
    {
        var now = DateTimeOffset.UtcNow;
        var match = await TrainingSessions.LoadAsync(matches, request.UserId, request.MatchId, now);
        if (match is null)
            return new(TrainingSessionOutcome.NotFound);
        if (match.Status == MatchStatus.Completed)
            return new(TrainingSessionOutcome.Success, TrainingMatchViews.Create(match, now));
        if (!match.MoveNext(now))
            return new(TrainingSessionOutcome.InvalidState);
        await matches.UpdateAsync(match);
        return new(TrainingSessionOutcome.Success, TrainingMatchViews.Create(match, now));
    }
}

public sealed class LeaveTrainingMatchHandler(IMatchRepository matches) : IHandler<LeaveTrainingMatchCommand, TrainingSessionResult>
{
    public async Task<TrainingSessionResult> HandleAsync(LeaveTrainingMatchCommand request)
    {
        var now = DateTimeOffset.UtcNow;
        var match = await TrainingSessions.LoadAsync(matches, request.UserId, request.MatchId, now);
        if (match is null)
            return new(TrainingSessionOutcome.NotFound);
        if (match.Status is not (MatchStatus.InProgress or MatchStatus.Completed))
            return new(TrainingSessionOutcome.InvalidState);
        if (match.Status == MatchStatus.InProgress)
        {
            match.Leave(now);
            await matches.UpdateAsync(match);
        }
        return new(TrainingSessionOutcome.Success, TrainingMatchViews.Create(match, now));
    }
}

public sealed class SaveTrainingDraftHandler(IMatchRepository matches) : IHandler<SaveTrainingDraftCommand, TrainingSessionResult>
{
    public async Task<TrainingSessionResult> HandleAsync(SaveTrainingDraftCommand request)
    {
        var now = DateTimeOffset.UtcNow;
        var match = await TrainingSessions.LoadAsync(matches, request.UserId, request.MatchId, now);
        if (match is null)
            return new(TrainingSessionOutcome.NotFound);
        var problem = match.Problems.SingleOrDefault(problem => problem.Id == request.MatchProblemId);
        if (match.Status != MatchStatus.InProgress || problem?.Status != MatchProblemStatus.Active)
            return new(TrainingSessionOutcome.InvalidState);
        if (!problem.ProblemVersion.Languages.Any(language => language.ProgrammingLanguageId == request.ProgrammingLanguageId &&
            language.ProgrammingLanguage.IsEnabled))
            return new(TrainingSessionOutcome.LanguageUnavailable);
        var draft = problem.Drafts.SingleOrDefault(draft => draft.ProgrammingLanguageId == request.ProgrammingLanguageId);
        if (draft is null)
        {
            draft = new CodeDraft { MatchProblemId = problem.Id, ProgrammingLanguageId = request.ProgrammingLanguageId, SourceCode = request.SourceCode };
            problem.Drafts.Add(draft);
        }
        draft.SourceCode = request.SourceCode;
        draft.CustomInput = request.StandardInput;
        draft.UpdatedAt = now;
        await matches.UpdateAsync(match);
        return new(TrainingSessionOutcome.Success);
    }
}
