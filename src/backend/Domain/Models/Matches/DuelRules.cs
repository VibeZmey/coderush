using Domain.Models.Execution;

namespace Domain.Models.Matches;

public enum DuelRank { Intern, Junior, Middle, Senior }

public enum DuelOutcome { FirstPlayerWon, SecondPlayerWon, Draw, JudgeUnavailable }

public sealed record DuelResult(DuelOutcome Outcome, int FirstPlayerDelta, int SecondPlayerDelta);

public static class DuelRules
{
    public const int InitialRating = 1000;
    public static readonly TimeSpan Countdown = TimeSpan.FromSeconds(5);
    public static readonly TimeSpan RankedDuration = TimeSpan.FromMinutes(15);

    public static (DuelRank Rank, int? NextThreshold) GetRank(int rating) => rating switch
    {
        < 1200 => (DuelRank.Intern, 1200),
        < 1400 => (DuelRank.Junior, 1400),
        < 1600 => (DuelRank.Middle, 1600),
        _ => (DuelRank.Senior, null)
    };

    // Null means play or judging is still in progress.
    public static DuelResult? Resolve(
        IEnumerable<Submission> firstPlayer,
        IEnumerable<Submission> secondPlayer,
        DateTimeOffset startsAt,
        DateTimeOffset deadline,
        DateTimeOffset now,
        bool ranked)
    {
        var first = Eligible(firstPlayer, startsAt, deadline);
        var second = Eligible(secondPlayer, startsAt, deadline);
        var attempts = ThroughFirstSolution(first).Concat(ThroughFirstSolution(second)).ToArray();
        if (now < deadline && !attempts.Any(IsAccepted))
            return null;

        // Ranked scoring distinguishes one solver from two. A later accepted
        // submission already in flight can change the delta even if it cannot win.
        if (attempts.Any(x => x.Status is ProcessingStatus.Pending or ProcessingStatus.Queued or ProcessingStatus.Running))
            return null;
        if (attempts.Any(x => x.Status is ProcessingStatus.Failed or ProcessingStatus.Cancelled))
            return new(DuelOutcome.JudgeUnavailable, 0, 0);

        var left = Summarize(first);
        var right = Summarize(second);
        int comparison;
        int baseDelta;
        if (left.SolvedAt.HasValue && right.SolvedAt.HasValue)
        {
            comparison = right.SolvedAt.Value.CompareTo(left.SolvedAt.Value);
            baseDelta = 23;
        }
        else if (left.SolvedAt.HasValue || right.SolvedAt.HasValue)
        {
            comparison = left.SolvedAt.HasValue ? 1 : -1;
            baseDelta = 25;
        }
        else if (left.PassedTests != right.PassedTests)
        {
            comparison = left.PassedTests.CompareTo(right.PassedTests);
            baseDelta = 21;
        }
        else
        {
            comparison = left.PassedTests == 0 ? 0 : right.Errors.CompareTo(left.Errors);
            baseDelta = 20;
        }

        if (comparison == 0)
            return new(DuelOutcome.Draw, 0, 0);

        var firstWon = comparison > 0;
        var winner = firstWon ? left : right;
        var delta = ranked ? Math.Max(18, baseDelta - winner.Errors) : 0;
        return firstWon
            ? new(DuelOutcome.FirstPlayerWon, delta, -delta)
            : new(DuelOutcome.SecondPlayerWon, -delta, delta);
    }

    private static Submission[] Eligible(
        IEnumerable<Submission> attempts, DateTimeOffset startsAt, DateTimeOffset deadline) =>
        attempts.Where(x => x.SubmittedAt >= startsAt && x.SubmittedAt < deadline)
            .OrderBy(x => x.SubmittedAt).ThenBy(x => x.Id).ToArray();

    private static IEnumerable<Submission> ThroughFirstSolution(Submission[] attempts)
    {
        var accepted = Array.FindIndex(attempts, IsAccepted);
        return accepted < 0 ? attempts : attempts.Take(accepted + 1);
    }

    internal static bool IsAccepted(Submission attempt) =>
        attempt.Status == ProcessingStatus.Completed && attempt.Verdict == ExecutionVerdict.Accepted;

    private static (DateTimeOffset? SolvedAt, int PassedTests, int Errors) Summarize(Submission[] attempts)
    {
        var accepted = Array.FindIndex(attempts, IsAccepted);
        if (accepted >= 0)
            return (attempts[accepted].SubmittedAt, 0, accepted);

        var best = 0;
        var errorsBeforeBest = 0;
        for (var index = 0; index < attempts.Length; index++)
        {
            var passed = attempts[index].TestResults.Count(x => x.Verdict == ExecutionVerdict.Accepted);
            if (passed > best)
            {
                best = passed;
                errorsBeforeBest = index;
            }
        }

        return (null, best, errorsBeforeBest);
    }
}



