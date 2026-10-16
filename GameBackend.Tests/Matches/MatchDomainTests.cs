using GameBackend.Domain;
using GameBackend.Domain.Matches;

namespace GameBackend.Tests.Matches;

/// <summary>Unit tests for match rules: no database, Redis or network.</summary>
[TestFixture]
public class MatchDomainTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private static readonly MatchTimings Timings = new(
        LoadingTimeout: TimeSpan.FromSeconds(20),
        Countdown: TimeSpan.FromSeconds(3),
        Round: TimeSpan.FromSeconds(10),
        ClickGrace: TimeSpan.FromMilliseconds(500),
        RematchTimeout: TimeSpan.FromSeconds(30));

    private static readonly Guid Alice = Guid.NewGuid();
    private static readonly Guid Bob = Guid.NewGuid();

    private static Match NewMatch() => Match.Create([(Alice, "alice"), (Bob, "bob")], Timings, Now);

    private static Match RunningMatch()
    {
        var match = NewMatch();
        match.MarkLoaded(Alice, Now);
        match.MarkLoaded(Bob, Now);
        return match;
    }

    private static Match FinishedMatch(long aliceScore = 5, long bobScore = 3)
    {
        var match = RunningMatch();
        match.Finish(new Dictionary<Guid, long> { [Alice] = aliceScore, [Bob] = bobScore }, Now.AddSeconds(14));
        return match;
    }

    [Test]
    public void MarkLoaded_LastPlayer_StartsCountdown()
    {
        var match = NewMatch();

        var first = match.MarkLoaded(Alice, Now);
        var second = match.MarkLoaded(Bob, Now);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(MatchTransition.None));
            Assert.That(second, Is.EqualTo(MatchTransition.Started));
            Assert.That(match.StartsAt, Is.EqualTo(Now + Timings.Countdown));
            Assert.That(match.EndsAt, Is.EqualTo(Now + Timings.Countdown + Timings.Round));
        });
    }

    [Test]
    public void LoadingTimeout_DropsPlayersWhoDidNotLoad()
    {
        var match = NewMatch();
        match.MarkLoaded(Alice, Now);

        var transition = match.OnDeadline(Now + Timings.LoadingTimeout, out var dropped);

        Assert.Multiple(() =>
        {
            Assert.That(transition, Is.EqualTo(MatchTransition.Started));
            Assert.That(dropped, Is.EqualTo(new[] { Bob }));
            Assert.That(match.ActivePlayerIds, Is.EqualTo(new[] { Alice }));
        });
    }

    [Test]
    public void AcceptsClick_OnlyDuringRound()
    {
        var match = RunningMatch();
        var startsAt = match.StartsAt!.Value;
        var endsAt = match.EndsAt!.Value;

        Assert.Multiple(() =>
        {
            Assert.That(match.AcceptsClickFrom(Alice, startsAt.AddMilliseconds(-1)), Is.False, "during the countdown");
            Assert.That(match.AcceptsClickFrom(Alice, startsAt.AddSeconds(5)), Is.True, "during the round");
            Assert.That(match.AcceptsClickFrom(Alice, endsAt + Timings.ClickGrace), Is.True, "within the network latency grace period");
            Assert.That(match.AcceptsClickFrom(Alice, endsAt.AddSeconds(1)), Is.False, "after the round");
        });
    }

    [Test]
    public void Finish_MarksAllPlayersWithBestScoreAsWinners()
    {
        var match = FinishedMatch(aliceScore: 7, bobScore: 7);

        Assert.That(match.GetWinners(), Is.EquivalentTo(new[] { Alice, Bob }));
    }

    [Test]
    public void Finish_NobodyClicked_NoWinners()
    {
        var match = FinishedMatch(aliceScore: 0, bobScore: 0);

        Assert.That(match.GetWinners(), Is.Empty);
    }

    [Test]
    public void Rematch_StartsOnlyWhenEveryoneAgrees()
    {
        var match = FinishedMatch();

        var first = match.RequestRematch(Alice);
        var second = match.RequestRematch(Bob);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(MatchTransition.RematchUpdated));
            Assert.That(second, Is.EqualTo(MatchTransition.RematchReady));
        });
    }

    [Test]
    public void Leave_AfterFinish_WithOnePlayerLeft_ClosesMatch()
    {
        var match = FinishedMatch();

        var transition = match.Leave(Bob, Now.AddSeconds(15));

        Assert.Multiple(() =>
        {
            Assert.That(transition, Is.EqualTo(MatchTransition.Closed));
            Assert.That(match.Phase, Is.EqualTo(MatchPhase.Closed));
        });
    }

    [Test]
    public void Leave_DuringLoading_StartsIfOthersAreLoaded()
    {
        var match = NewMatch();
        match.MarkLoaded(Alice, Now);

        var transition = match.Leave(Bob, Now);

        Assert.That(transition, Is.EqualTo(MatchTransition.Started));
    }

    [Test]
    public void RequestRematch_BeforeFinish_Throws()
    {
        var match = RunningMatch();

        Assert.Throws<DomainException>(() => match.RequestRematch(Alice));
    }
}
