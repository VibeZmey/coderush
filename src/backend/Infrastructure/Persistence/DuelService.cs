using Domain.Models.Execution;
using Domain.Models.Matches;
using Domain.Models.Problems;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public sealed record DuelRatingView(int Rating, string Rank, int? NextThreshold, string? LastResult, int? LastDelta);
public sealed record DuelAttemptView(Guid Id, string Status, string? Verdict);
public sealed record DuelLanguageView(int Id, string Name, string Template);
public sealed record DuelSampleView(string Input, string ExpectedOutput);
public sealed record DuelProblemView(Guid Id, string Title, string Description, string? Input, string? Output,
    string? Constraints, string? Explanation, double CpuTimeLimitSeconds, int MemoryLimitKilobytes,
    DuelLanguageView[] Languages, DuelSampleView[] Samples);
public sealed record DuelMatchView(Guid Id, bool Ranked, string Status, DateTimeOffset? StartsAt, DateTimeOffset? Deadline,
    string? Opponent, int? OpponentRating, string? OpponentRank,
    DuelProblemView? Problem, DuelAttemptView[] OwnAttempts, DuelAttemptView[] OpponentAttempts, string? Result, int Delta,
    DateTimeOffset? AcceptBy, bool Accepted, bool OpponentAccepted);
public sealed record DuelState(DateTimeOffset ServerTime, bool Searching, string? SearchMessage, DuelRatingView Rating, DuelMatchView? Match);
public sealed record DuelActionResult(bool Success, string? Message = null);
public sealed record DuelCodeView(string SourceCode, string Language, bool Solved);
public sealed record DuelProfileView(string UserName, DuelRatingView Rating);

public sealed class DuelService(AppDbContext db)
{
    private IQueryable<DuelMatch> Matches => db.Set<DuelMatch>();
    private DbSet<DuelQueueEntry> Queue => db.Set<DuelQueueEntry>();
    private static readonly TimeSpan PresenceWindow = TimeSpan.FromSeconds(15);

    // Serializes pairing, acceptance, verdict transitions and result persistence across instances.
    internal static Task LockAsync(AppDbContext db, CancellationToken ct) =>
        db.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(524932006)", ct);

    public async Task<DuelRatingView> RatingAsync(string userId, CancellationToken ct)
    {
        var history = RatedHistory(userId);
        var summary = await history.OrderByDescending(x => x.CompletedAt).ThenByDescending(x => x.Id)
            .Select(last => new { Last = last, Delta = history.Sum(x => x.UserId == userId ? x.FirstDelta : x.SecondDelta) })
            .FirstOrDefaultAsync(ct);
        var rating = DuelRules.InitialRating + (summary?.Delta ?? 0);
        var last = summary?.Last;
        var rank = DuelRules.GetRank(rating);
        return new(rating, rank.Rank.ToString(), rank.NextThreshold,
            last is null ? null : ResultFor(last, userId), last is null ? null : DeltaFor(last, userId));
    }

    private IQueryable<DuelMatch> RatedHistory(string userId) =>
        Matches.AsNoTracking().Where(x => x.Ranked && x.Outcome != null &&
            x.Outcome != DuelOutcome.JudgeUnavailable && (x.UserId == userId || x.OpponentId == userId));

    private async Task<int> RatingValueAsync(string userId, CancellationToken ct) =>
        DuelRules.InitialRating + await RatedHistory(userId).SumAsync(x => x.UserId == userId ? x.FirstDelta : x.SecondDelta, ct);

    public Task<DuelActionResult> SearchAsync(string userId, CancellationToken ct) =>
        MutateAsync(async now =>
        {
            if (await ActiveAsync(userId, ct) is not null)
                return new(false, "Сначала заверши текущую дуэль.");

            var entry = await Queue.FindAsync([userId], ct);
            if (entry is null)
                Queue.Add(new() { UserId = userId, EnqueuedAt = now, SeenAt = now });
            else
            {
                if (entry.SeenAt < now - PresenceWindow)
                    entry.EnqueuedAt = now;
                entry.SeenAt = now;
            }

            return new(true);
        }, ct);

    public Task<DuelActionResult> CancelAsync(string userId, CancellationToken ct) =>
        MutateAsync(async _ =>
        {
            await Queue.Where(x => x.UserId == userId).ExecuteDeleteAsync(ct);
            var lobby = await ActiveAsync(userId, ct);
            if (lobby?.Status == MatchStatus.Created && lobby.AcceptBy is null)
                lobby.Status = MatchStatus.Cancelled;
            return new(true);
        }, ct);

    public Task<DuelActionResult> CreateAsync(string userId, int durationMinutes, ProblemDifficulty difficulty, CancellationToken ct) =>
        MutateAsync(async now =>
        {
            if (durationMinutes is < 1 or > 60 || difficulty is < ProblemDifficulty.Easy or > ProblemDifficulty.Hard)
                return new(false, "Выбери сложность и длительность от 1 до 60 минут.");
            if (await ActiveAsync(userId, ct) is not null)
                return new(false, "Сначала заверши текущую дуэль.");
            var problem = await PickProblemAsync(difficulty, ct);
            if (problem is null)
                return new(false, "Нет опубликованных задач выбранной сложности.");

            await Queue.Where(x => x.UserId == userId).ExecuteDeleteAsync(ct);
            var rating = await RatingValueAsync(userId, ct);
            db.Add(CreateMatch(userId, rating, false, durationMinutes * 60, problem.Value, now));
            return new(true);
        }, ct);

    public Task<DuelActionResult> JoinAsync(Guid id, string userId, CancellationToken ct) =>
        MutateAsync(async now =>
        {
            var match = await Matches.Include(x => x.Problems).SingleOrDefaultAsync(x => x.Id == id && !x.Ranked, ct);
            if (match is not null && match.HasPlayer(userId) && match.Status != MatchStatus.Cancelled)
                return new(true);
            if (match is null || match.Status != MatchStatus.Created || match.OpponentId is not null)
                return new(false, "Лобби закрыто или уже заполнено.");
            if (await ActiveAsync(userId, ct) is not null)
                return new(false, "Сначала заверши текущую дуэль.");

            await Queue.Where(x => x.UserId == userId).ExecuteDeleteAsync(ct);
            match.FirstRating = await RatingValueAsync(match.UserId, ct);
            match.Offer(userId, await RatingValueAsync(userId, ct), now);
            return new(true);
        }, ct);

    public Task<DuelActionResult> AcceptAsync(Guid id, string userId, CancellationToken ct) =>
        MutateAsync(async now =>
        {
            var match = await Matches.Include(x => x.Problems).SingleOrDefaultAsync(x => x.Id == id &&
                (x.UserId == userId || x.OpponentId == userId), ct);
            return match?.Accept(userId, now) == true
                ? new(true)
                : new(false, "Время на принятие матча истекло.");
        }, ct);

    public async Task<DuelState> StateAsync(string userId, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await LockAsync(db, ct);
        var now = DateTimeOffset.UtcNow;
        var match = await ActiveAsync(userId, ct);
        if (match is not null && await ExpireOfferAsync(match, now, ct))
        {
            await db.SaveChangesAsync(ct);
            match = null;
        }
        string? searchMessage = null;

        var entry = await Queue.FindAsync([userId], ct);
        var searching = entry is not null && entry.SeenAt >= now - PresenceWindow;
        if (match is null && searching)
        {
            entry!.SeenAt = now;
            (match, searchMessage) = await FindOpponentAsync(entry, now, ct);
            searching = match is null;
        }

        if (match is null && !searching)
            match = await Matches.Where(x => (x.Status == MatchStatus.Completed ||
                (x.Status == MatchStatus.Cancelled && x.AcceptBy != null)) &&
                (x.UserId == userId || x.OpponentId == userId))
                .OrderByDescending(x => x.CompletedAt).ThenByDescending(x => x.Id).FirstOrDefaultAsync(ct);
        if (match?.Status == MatchStatus.Cancelled)
            match = null;
        if (match is not null && match.Status is not (MatchStatus.Completed or MatchStatus.Cancelled))
        {
            if (match.UserId == userId) match.FirstSeenAt = now;
            else match.SecondSeenAt = now;
        }
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        var view = match is null ? null : await ViewAsync(match, userId, now, ct);
        return new(now, searching, searchMessage, await RatingAsync(userId, ct), view);
    }

    public async Task SweepAsync(CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        var ids = await Matches.AsNoTracking().Where(x =>
            (x.Status == MatchStatus.Created && x.AcceptBy <= now) ||
            (x.Status == MatchStatus.InProgress && x.Problems.Any(p => p.SubmissionDeadlineAt <= now)))
            .Select(x => x.Id).Take(100).ToArrayAsync(ct);
        if (ids.Length == 0)
            return;

        await MutateAsync(async time =>
        {
            var matches = await Matches.Where(x => ids.Contains(x.Id) &&
                (x.Status == MatchStatus.InProgress || x.Status == MatchStatus.Created))
                .Include(x => x.Problems).ThenInclude(x => x.Submissions).ThenInclude(x => x.TestResults)
                .AsSplitQuery().ToListAsync(ct);
            foreach (var match in matches)
            {
                await ExpireOfferAsync(match, time, ct);
                match.Refresh(time);
            }
            return new(true);
        }, ct);
    }

    private async Task<bool> ExpireOfferAsync(DuelMatch match, DateTimeOffset now, CancellationToken ct)
    {
        if (!match.ExpireOffer(now))
            return false;
        if (!match.Ranked || match.FirstAccepted == match.SecondAccepted)
            return true;

        var player = match.FirstAccepted ? match.UserId : match.OpponentId!;
        var seen = match.FirstAccepted ? match.FirstSeenAt : match.SecondSeenAt;
        if (seen >= now - PresenceWindow && await Queue.FindAsync([player], ct) is null)
            Queue.Add(new() { UserId = player, EnqueuedAt = match.CreatedAt, SeenAt = now });
        return true;
    }

    public Task<DuelActionResult> SaveDraftAsync(Guid id, string userId, int languageId, string source, CancellationToken ct) =>
        MutateAsync(async now =>
        {
            var match = await Matches.Include(x => x.Problems).SingleOrDefaultAsync(x => x.Id == id &&
                (x.UserId == userId || x.OpponentId == userId), ct);
            var assignment = match?.Problems.Single();
            if (match?.Status != MatchStatus.InProgress || assignment?.StartedAt is null || assignment.StartedAt > now ||
                assignment.SubmissionDeadlineAt is null || assignment.SubmissionDeadlineAt <= now)
                return new(false);
            if (!await db.ProblemLanguages.AnyAsync(x => x.ProblemVersionId == assignment.ProblemVersionId &&
                x.ProgrammingLanguageId == languageId && x.ProgrammingLanguage.IsEnabled, ct))
                return new(false);

            var drafts = db.Set<DuelDraft>();
            var draft = await drafts.FindAsync([id, userId], ct);
            if (draft is null)
                drafts.Add(new() { MatchId = id, UserId = userId, ProgrammingLanguageId = languageId, SourceCode = source, UpdatedAt = now });
            else
            {
                draft.ProgrammingLanguageId = languageId;
                draft.SourceCode = source;
                draft.UpdatedAt = now;
            }
            return new(true);
        }, ct);

    public async Task<DuelCodeView?> OpponentCodeAsync(Guid id, string userId, CancellationToken ct)
    {
        var match = await CompletedMatchAsync(id, userId, ct);
        if (match is null)
            return null;

        var opponent = match.UserId == userId ? match.OpponentId : match.UserId;
        var attempts = db.Set<Submission>().AsNoTracking()
            .Where(x => x.MatchProblem.MatchId == id && x.DuelPlayerId == opponent);
        var solved = await attempts.Where(x => x.Status == ProcessingStatus.Completed && x.Verdict == ExecutionVerdict.Accepted)
            .OrderBy(x => x.SubmittedAt).ThenBy(x => x.Id)
            .Select(x => new DuelCodeView(x.SourceCode, x.ProgrammingLanguage.DisplayName, true)).FirstOrDefaultAsync(ct);
        if (solved is not null)
            return solved;

        var latest = await attempts.OrderByDescending(x => x.SubmittedAt).ThenByDescending(x => x.Id)
            .Select(x => new { x.SubmittedAt, Code = new DuelCodeView(x.SourceCode, x.ProgrammingLanguage.DisplayName, false) })
            .FirstOrDefaultAsync(ct);
        var lastSubmittedAt = latest?.SubmittedAt ?? DateTimeOffset.MinValue;
        var draft = await db.Set<DuelDraft>().AsNoTracking().Where(x => x.MatchId == id && x.UserId == opponent && x.SourceCode != "" &&
            x.UpdatedAt >= lastSubmittedAt)
            .Select(x => new DuelCodeView(x.SourceCode, x.ProgrammingLanguage.DisplayName, false)).FirstOrDefaultAsync(ct);
        return draft ?? latest?.Code;
    }

    public async Task<DuelProfileView?> OpponentProfileAsync(Guid id, string userId, CancellationToken ct)
    {
        var match = await CompletedMatchAsync(id, userId, ct);
        if (match is null)
            return null;
        var opponent = match.UserId == userId ? match.OpponentId! : match.UserId;
        var name = await db.Users.Where(x => x.Id == opponent).Select(x => x.UserName).SingleAsync(ct);
        return new(name ?? "Соперник", await RatingAsync(opponent, ct));
    }

    private Task<DuelMatch?> CompletedMatchAsync(Guid id, string userId, CancellationToken ct) =>
        Matches.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.Status == MatchStatus.Completed &&
            (x.UserId == userId || x.OpponentId == userId), ct);

    private async Task<DuelActionResult> MutateAsync(Func<DateTimeOffset, Task<DuelActionResult>> action, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await LockAsync(db, ct);
        var result = await action(DateTimeOffset.UtcNow);
        if (result.Success)
        {
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        return result;
    }

    private Task<DuelMatch?> ActiveAsync(string userId, CancellationToken ct) =>
        Matches.SingleOrDefaultAsync(x => x.Status != MatchStatus.Completed && x.Status != MatchStatus.Cancelled &&
            (x.UserId == userId || x.OpponentId == userId), ct);

    private async Task<(DuelMatch? Match, string? Message)> FindOpponentAsync(DuelQueueEntry entry, DateTimeOffset now, CancellationToken ct)
    {
        var rating = await RatingValueAsync(entry.UserId, ct);
        var candidates = await Queue.Where(x => x.UserId != entry.UserId && x.SeenAt >= now - PresenceWindow)
            .OrderBy(x => x.EnqueuedAt).ToListAsync(ct);
        DuelQueueEntry? opponent = null;
        var opponentRating = 0;
        var closest = int.MaxValue;
        foreach (var candidate in candidates)
        {
            var other = await RatingValueAsync(candidate.UserId, ct);
            var wait = now - (entry.EnqueuedAt < candidate.EnqueuedAt ? entry.EnqueuedAt : candidate.EnqueuedAt);
            var range = 100 + 50 * Math.Min(8, (int)(wait.TotalSeconds / 15));
            var distance = Math.Abs(rating - other);
            if (distance > range || distance >= closest)
                continue;

            opponent = candidate;
            opponentRating = other;
            closest = distance;
        }
        if (opponent is null)
            return (null, null);

        var average = ((long)rating + opponentRating) / 2;
        var difficulty = average < 1200 ? ProblemDifficulty.Easy : average < 1400 ? ProblemDifficulty.Medium : ProblemDifficulty.Hard;
        var problem = await PickProblemAsync(difficulty, ct);
        if (problem is null)
            return (null, "Для рейтинга этой пары пока нет опубликованных задач.");

        var match = CreateMatch(entry.UserId, rating, true, (int)DuelRules.RankedDuration.TotalSeconds, problem.Value, now);
        match.Offer(opponent.UserId, opponentRating, now);
        match.SecondSeenAt = opponent.SeenAt;
        db.Add(match);
        Queue.RemoveRange(entry, opponent);
        return (match, null);
    }

    private Task<Guid?> PickProblemAsync(ProblemDifficulty difficulty, CancellationToken ct) =>
        db.ProblemVersions.Where(x => x.Difficulty == difficulty && x.Status == PublicationStatus.Published &&
            !x.Problem.IsArchived && x.TestCases.Any() && x.Languages.Any(l => l.ProgrammingLanguage.IsEnabled) &&
            !db.ProblemVersions.Any(newer => newer.ProblemId == x.ProblemId &&
                newer.Status == PublicationStatus.Published && newer.VersionNumber > x.VersionNumber))
            .OrderBy(_ => EF.Functions.Random()).Select(x => (Guid?)x.Id).FirstOrDefaultAsync(ct);

    private static DuelMatch CreateMatch(string userId, int rating, bool ranked, int duration, Guid problem, DateTimeOffset now) =>
        new()
        {
            UserId = userId, FirstRating = rating, FirstSeenAt = now, Ranked = ranked, DurationSeconds = duration,
            Problems = [new MatchProblem { ProblemVersionId = problem, Position = 0 }]
        };

    private async Task<DuelMatchView> ViewAsync(DuelMatch match, string userId, DateTimeOffset now, CancellationToken ct)
    {
        var first = match.UserId == userId;
        var opponentId = first ? match.OpponentId : match.UserId;
        var opponentRating = first ? match.SecondRating : match.FirstRating;
        var opponent = opponentId is null ? null : await db.Users.Where(x => x.Id == opponentId).Select(x => x.UserName).SingleAsync(ct);
        var status = match.Status.ToString();
        DuelProblemView? problem = null;
        DuelAttemptView[] own = [], other = [];
        if (match.StartedAt > now)
            status = "Countdown";
        else if (match.Status == MatchStatus.Created && match.AcceptBy is not null)
            status = "AwaitingAcceptance";
        else if (match.StartedAt is not null)
        {
            var assignment = await db.MatchProblems.AsNoTracking().Where(x => x.MatchId == match.Id)
                .Include(x => x.ProblemVersion).ThenInclude(x => x.Languages).ThenInclude(x => x.ProgrammingLanguage)
                .Include(x => x.ProblemVersion).ThenInclude(x => x.TestCases.Where(test => test.IsSample))
                .AsSplitQuery().SingleAsync(ct);
            var version = assignment.ProblemVersion;
            problem = new(assignment.Id, version.Title, version.DescriptionMarkdown, version.InputDescriptionMarkdown,
                version.OutputDescriptionMarkdown, version.ConstraintsMarkdown, version.ExplanationMarkdown,
                version.CpuTimeLimitSeconds, version.MemoryLimitKilobytes,
                version.Languages.Where(x => x.ProgrammingLanguage.IsEnabled).OrderBy(x => x.ProgrammingLanguageId)
                    .Select(x => new DuelLanguageView(x.ProgrammingLanguageId, x.ProgrammingLanguage.DisplayName, x.Template)).ToArray(),
                version.TestCases.OrderBy(x => x.Position)
                    .Select(x => new DuelSampleView(x.Input, x.ExpectedOutput)).ToArray());
            var attempts = await db.Set<Submission>().AsNoTracking().Where(x => x.MatchProblemId == assignment.Id)
                .OrderByDescending(x => x.SubmittedAt).ThenByDescending(x => x.Id)
                .Select(x => new { x.Id, x.DuelPlayerId, x.Status, x.Verdict }).ToListAsync(ct);
            own = attempts.Where(x => x.DuelPlayerId == userId)
                .Select(x => new DuelAttemptView(x.Id, x.Status.ToString(), x.Verdict?.ToString())).ToArray();
            other = attempts.Where(x => x.DuelPlayerId == opponentId)
                .Select(x => new DuelAttemptView(x.Id, x.Status.ToString(), x.Verdict?.ToString())).ToArray();
        }

        return new(match.Id, match.Ranked, status, match.StartedAt, match.StartedAt?.AddSeconds(match.DurationSeconds),
            opponent, opponentId is null ? null : opponentRating, opponentId is null ? null : DuelRules.GetRank(opponentRating).Rank.ToString(),
            problem, own, other, ResultFor(match, userId), DeltaFor(match, userId),
            match.AcceptBy, first ? match.FirstAccepted : match.SecondAccepted, first ? match.SecondAccepted : match.FirstAccepted);
    }

    private static int DeltaFor(DuelMatch match, string userId) => match.UserId == userId ? match.FirstDelta : match.SecondDelta;

    private static string? ResultFor(DuelMatch match, string userId) => match.Outcome switch
    {
        DuelOutcome.FirstPlayerWon => match.UserId == userId ? "Win" : "Loss",
        DuelOutcome.SecondPlayerWon => match.OpponentId == userId ? "Win" : "Loss",
        DuelOutcome.Draw => "Draw",
        DuelOutcome.JudgeUnavailable => "JudgeUnavailable",
        _ => null
    };
}

