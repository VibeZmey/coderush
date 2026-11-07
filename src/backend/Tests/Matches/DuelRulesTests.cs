using Domain.Models.Execution;
using Domain.Models.Matches;
using Xunit;

namespace Tests.Matches;

public sealed class DuelRulesTests
{
    private static readonly DateTimeOffset Start = DateTimeOffset.UnixEpoch;
    private static readonly DateTimeOffset Deadline = Start + DuelRules.RankedDuration;

    [Fact]
    public void EarlierSubmissionCanWinAfterLaterSubmissionIsJudged()
    {
        var earlier = Attempt(10, ExecutionVerdict.Accepted);
        var later = Attempt(20, ExecutionVerdict.Accepted);
        earlier.Status = ProcessingStatus.Running;

        Assert.Null(DuelRules.Resolve([earlier], [later], Start, Deadline, Start.AddSeconds(30), true));

        earlier.Status = ProcessingStatus.Completed;
        Assert.Equal(new DuelResult(DuelOutcome.FirstPlayerWon, 23, -23),
            DuelRules.Resolve([earlier], [later], Start, Deadline, Start.AddSeconds(30), true));
    }

    [Fact]
    public void MatchClosesAcceptanceOnFirstVerifiedSolveAndPersistsOneResult()
    {
        var problem = new MatchProblem();
        var match = new DuelMatch
        {
            UserId = "first", Ranked = true, DurationSeconds = 900, Problems = [problem]
        };
        match.Offer("second", 1000, Start);
        Assert.False(match.Accept("outsider", Start));
        Assert.True(match.Accept("first", Start));
        Assert.Null(problem.StartedAt);
        Assert.True(match.Accept("second", Start));
        Assert.Equal(Start.AddSeconds(5), problem.StartedAt);
        Assert.Equal(Start.AddSeconds(905), problem.SubmissionDeadlineAt);
        var earlier = Attempt(10, ExecutionVerdict.Accepted);
        earlier.DuelPlayerId = "first";
        earlier.Status = ProcessingStatus.Running;
        var later = Attempt(20, ExecutionVerdict.Accepted);
        later.DuelPlayerId = "second";
        problem.Submissions = [earlier, later];

        match.Refresh(Start.AddSeconds(25));
        Assert.Equal(MatchStatus.Finalizing, match.Status);
        Assert.Null(match.Outcome);

        earlier.Status = ProcessingStatus.Completed;
        match.Refresh(Start.AddSeconds(30));
        match.Refresh(Start.AddSeconds(31));
        Assert.Equal(MatchStatus.Completed, match.Status);
        Assert.Equal(DuelOutcome.FirstPlayerWon, match.Outcome);
        Assert.Equal((23, -23), (match.FirstDelta, match.SecondDelta));
        Assert.Equal(Start.AddSeconds(30), match.CompletedAt);
    }

    [Fact]
    public void UnacceptedOfferExpiresWithoutStartingTheMatch()
    {
        var match = new DuelMatch { UserId = "first", Problems = [new MatchProblem()] };
        match.Offer("second", 1000, Start);
        Assert.True(match.Accept("first", Start.AddSeconds(29)));
        Assert.False(match.Accept("second", Start.AddSeconds(30)));
        match.ExpireOffer(Start.AddSeconds(30));
        Assert.Equal(MatchStatus.Cancelled, match.Status);
        Assert.Null(match.StartedAt);
        Assert.Null(match.Outcome);
    }

    [Fact]
    public void WrongAttemptsReduceRatingGainWithoutAddingTime()
    {
        var attempts = Enumerable.Range(1, 10)
            .Select(second => Attempt(second, ExecutionVerdict.WrongAnswer))
            .Append(Attempt(20, ExecutionVerdict.Accepted));

        Assert.Equal(new DuelResult(DuelOutcome.FirstPlayerWon, 18, -18),
            DuelRules.Resolve(attempts, [], Start, Deadline, Start.AddSeconds(30), true));
    }

    [Fact]
    public void TimeoutUsesBestSingleSubmissionAndFewerErrorsBreakTheTie()
    {
        var first = new[] { Attempt(10, ExecutionVerdict.WrongAnswer, 2), Attempt(20, ExecutionVerdict.WrongAnswer, 2) };
        var second = new[] { Attempt(10, ExecutionVerdict.WrongAnswer, 1), Attempt(20, ExecutionVerdict.WrongAnswer, 2) };
        Assert.Null(DuelRules.Resolve(first, second, Start, Deadline, Deadline.AddSeconds(-1), true));
        Assert.Equal(new DuelResult(DuelOutcome.FirstPlayerWon, 20, -20),
            DuelRules.Resolve(first, second, Start, Deadline, Deadline, true));
        Assert.Equal(new DuelResult(DuelOutcome.FirstPlayerWon, 0, 0),
            DuelRules.Resolve(first, second, Start, Deadline, Deadline, false));
    }

    [Fact]
    public void DeadlineExcludesLateSolutionsAndWaitsForEligibleChecks()
    {
        var pending = Attempt(20, ExecutionVerdict.WrongAnswer, 2);
        pending.Status = ProcessingStatus.Running;
        var late = Attempt(900, ExecutionVerdict.Accepted);
        Assert.Null(DuelRules.Resolve([pending], [late], Start, Deadline, Deadline, true));

        pending.Status = ProcessingStatus.Completed;
        Assert.Equal(new DuelResult(DuelOutcome.FirstPlayerWon, 21, -21),
            DuelRules.Resolve([pending], [late], Start, Deadline, Deadline, true));
    }

    [Fact]
    public void TechnicalFailureThatMayChangeOutcomeDoesNotAwardRating()
    {
        var failed = Attempt(5, ExecutionVerdict.WrongAnswer);
        failed.Status = ProcessingStatus.Failed;
        Assert.Equal(new DuelResult(DuelOutcome.JudgeUnavailable, 0, 0),
            DuelRules.Resolve([failed], [Attempt(10, ExecutionVerdict.Accepted)], Start, Deadline, Start.AddSeconds(15), true));
    }

    [Fact]
    public void EqualResultsAndNoProgressAreDraws()
    {
        var error = Attempt(5, ExecutionVerdict.CompilationError);
        Assert.Equal(new DuelResult(DuelOutcome.Draw, 0, 0),
            DuelRules.Resolve([error], [], Start, Deadline, Deadline, true));
        Assert.Equal(new DuelResult(DuelOutcome.Draw, 0, 0),
            DuelRules.Resolve([Attempt(5, ExecutionVerdict.Accepted)], [Attempt(5, ExecutionVerdict.Accepted)],
                Start, Deadline, Start.AddSeconds(15), true));
    }

    [Theory]
    [InlineData(1000, DuelRank.Intern, 1200)]
    [InlineData(1200, DuelRank.Junior, 1400)]
    [InlineData(1400, DuelRank.Middle, 1600)]
    [InlineData(1600, DuelRank.Senior, null)]
    public void RankThresholds(int rating, DuelRank rank, int? next) =>
        Assert.Equal((rank, next), DuelRules.GetRank(rating));

    private static Submission Attempt(int second, ExecutionVerdict verdict, int passed = 0) => new()
    {
        SourceCode = "",
        SubmittedAt = Start.AddSeconds(second),
        Status = ProcessingStatus.Completed,
        Verdict = verdict,
        TestResults = Enumerable.Range(0, passed).Select(_ => new SubmissionTestResult { Verdict = ExecutionVerdict.Accepted }).ToList()
    };
}

