namespace GameBackend.Domain.Matches;

public enum MatchPhase
{
    Loading,   // clients are loading the scene, waiting for everyone
    Running,   // countdown and round in progress
    Finished,  // results displayed, voting for a rematch underway
    Closed     // match concluded, data can be deleted
}

/// <summary>What changed after an action on a match. The service uses it to decide which events to send.</summary>
public enum MatchTransition
{
    None,
    Started,          // everyone has loaded: the countdown has started
    FinishDue,        // round has ended, time to tally the results
    RematchUpdated,   // someone has voted for a rematch or has left
    RematchReady,     // all remaining players want a rematch
    Closed            // match is closed (everyone has left, rematch timeout, etc.)
}

public record MatchTimings(
    TimeSpan LoadingTimeout,
    TimeSpan Countdown,
    TimeSpan Round,
    TimeSpan ClickGrace,
    TimeSpan RematchTimeout);

public class MatchParticipant(
    Guid playerId, string username,
    bool isLoaded = false, bool hasLeft = false, bool wantsRematch = false, long score = 0)
{
    public Guid PlayerId { get; } = playerId;
    public string Username { get; } = username;
    public bool IsLoaded { get; internal set; } = isLoaded;
    public bool HasLeft { get; internal set; } = hasLeft;
    public bool WantsRematch { get; internal set; } = wantsRematch;
    public long Score { get; internal set; } = score;
}

public class Match
{
    private const int MinPlayersForRematch = 2;

    private readonly List<MatchParticipant> _participants;

    private Match(Guid id, MatchPhase phase, MatchTimings timings, DateTime createdAt,
        DateTime deadline, DateTime? startsAt, DateTime? endsAt, IEnumerable<MatchParticipant> participants)
    {
        Id = id;
        Phase = phase;
        Timings = timings;
        CreatedAt = createdAt;
        Deadline = deadline;
        StartsAt = startsAt;
        EndsAt = endsAt;
        _participants = participants.ToList();
    }

    public Guid Id { get; }
    public MatchPhase Phase { get; private set; }
    public MatchTimings Timings { get; }
    public DateTime CreatedAt { get; }

    /// <summary>The moment when the server itself needs to advance the match (loading timeout, end of round, rematch timeout).</summary>
    public DateTime Deadline { get; private set; }

    public DateTime? StartsAt { get; private set; }
    public DateTime? EndsAt { get; private set; }

    public IReadOnlyList<MatchParticipant> Participants => _participants;
    public IEnumerable<MatchParticipant> Active => _participants.Where(p => !p.HasLeft);
    public IReadOnlyList<Guid> ActivePlayerIds => Active.Select(p => p.PlayerId).ToList();

    public static Match Create(IEnumerable<(Guid PlayerId, string Username)> players, MatchTimings timings, DateTime now)
    {
        var participants = players
            .DistinctBy(p => p.PlayerId)
            .Select(p => new MatchParticipant(p.PlayerId, p.Username))
            .ToList();

        if (participants.Count == 0)
            throw new DomainException("A match must have at least one player");

        return new Match(Guid.NewGuid(), MatchPhase.Loading, timings, now,
            now + timings.LoadingTimeout, null, null, participants);
    }

    public static Match Restore(Guid id, MatchPhase phase, MatchTimings timings, DateTime createdAt,
        DateTime deadline, DateTime? startsAt, DateTime? endsAt, IEnumerable<MatchParticipant> participants) =>
        new(id, phase, timings, createdAt, deadline, startsAt, endsAt, participants);

    public MatchParticipant? Find(Guid playerId) => _participants.FirstOrDefault(p => p.PlayerId == playerId);

    public MatchTransition MarkLoaded(Guid playerId, DateTime now)
    {
        if (Phase != MatchPhase.Loading)
            return MatchTransition.None; // late or repeat call — ignore

        var participant = GetActive(playerId);
        participant.IsLoaded = true;

        return TryStart(now);
    }

    public bool AcceptsClickFrom(Guid playerId, DateTime now) =>
        Phase == MatchPhase.Running
        && Find(playerId) is { HasLeft: false }
        && now >= StartsAt
        && now <= EndsAt + Timings.ClickGrace;

    /// <summary>Called when the <see cref="Deadline"/> has been reached.</summary>
    /// <param name="dropped">Players who were removed from the match due to a loading timeout.</param>
    public MatchTransition OnDeadline(DateTime now, out IReadOnlyList<Guid> dropped)
    {
        dropped = [];

        if (now < Deadline)
            return MatchTransition.None;

        switch (Phase)
        {
            case MatchPhase.Loading:
                var notLoaded = Active.Where(p => !p.IsLoaded).ToList();
                foreach (var p in notLoaded)
                    p.HasLeft = true;

                dropped = notLoaded.Select(p => p.PlayerId).ToList();
                return Active.Any() ? TryStart(now) : Close();

            case MatchPhase.Running:
                return MatchTransition.FinishDue;

            case MatchPhase.Finished:
                return Close(); // no one agreed to a rematch on time

            default:
                return MatchTransition.None;
        }
    }

    public void Finish(IReadOnlyDictionary<Guid, long> scores, DateTime now)
    {
        if (Phase != MatchPhase.Running)
            throw new DomainException("The match has not started yet or is already over");

        foreach (var p in _participants)
        {
            p.Score = scores.GetValueOrDefault(p.PlayerId);
            p.WantsRematch = false;
        }

        Phase = MatchPhase.Finished;
        Deadline = now + Timings.RematchTimeout;
    }

    /// <summary>The winners are all active players with the highest score (if it is greater than zero).</summary>
    public IReadOnlyList<Guid> GetWinners()
    {
        var active = Active.ToList();
        if (active.Count == 0)
            return [];

        var best = active.Max(p => p.Score);
        return best <= 0 ? [] : active.Where(p => p.Score == best).Select(p => p.PlayerId).ToList();
    }

    public MatchTransition RequestRematch(Guid playerId)
    {
        if (Phase != MatchPhase.Finished)
            throw new DomainException("A rematch can only be requested after the match has ended");

        GetActive(playerId).WantsRematch = true;
        return EvaluateRematch();
    }

    public MatchTransition Leave(Guid playerId, DateTime now)
    {
        var participant = Find(playerId);
        if (participant is null || participant.HasLeft || Phase == MatchPhase.Closed)
            return MatchTransition.None;

        participant.HasLeft = true;
        participant.WantsRematch = false;

        if (!Active.Any())
            return Close();

        return Phase switch
        {
            MatchPhase.Loading => TryStart(now),         // we may have been waiting only for the player who left
            MatchPhase.Finished => EvaluateRematch(),
            _ => MatchTransition.None
        };
    }

    /// <summary>A new match with the same active players.</summary>
    public Match CreateRematch(DateTime now)
    {
        if (Phase != MatchPhase.Finished)
            throw new DomainException("A rematch is only possible after the match has ended");

        return Create(Active.Select(p => (p.PlayerId, p.Username)), Timings, now);
    }

    public MatchTransition Close()
    {
        Phase = MatchPhase.Closed;
        return MatchTransition.Closed;
    }

    private MatchTransition TryStart(DateTime now)
    {
        if (Phase != MatchPhase.Loading || !Active.All(p => p.IsLoaded))
            return MatchTransition.None;

        Phase = MatchPhase.Running;
        StartsAt = now + Timings.Countdown;
        EndsAt = StartsAt + Timings.Round;
        Deadline = EndsAt!.Value + Timings.ClickGrace;
        return MatchTransition.Started;
    }

    private MatchTransition EvaluateRematch()
    {
        var active = Active.ToList();

        if (active.Count < MinPlayersForRematch)
            return Close();

        return active.All(p => p.WantsRematch) ? MatchTransition.RematchReady : MatchTransition.RematchUpdated;
    }

    private MatchParticipant GetActive(Guid playerId) =>
        Find(playerId) is { HasLeft: false } participant
            ? participant
            : throw new DomainException("You are not participating in this match");
}
