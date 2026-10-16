using GameBackend.Application.Abstractions;
using GameBackend.Application.Common;
using GameBackend.Application.Leaderboards;
using GameBackend.Domain.Matches;

namespace GameBackend.Application.Matches;

public class MatchService(
    IMatchStore store,
    IGameNotifier notifier,
    LeaderboardService leaderboards,
    MatchOptions options,
    TimeProvider timeProvider)
{
    private DateTime Now => timeProvider.GetUtcNow().UtcDateTime;

    public async Task<MatchStartingDto> CreateAsync(
        IReadOnlyList<(Guid PlayerId, string Username)> players, CancellationToken ct)
    {
        var match = Match.Create(players, options.ToTimings(), Now);
        return await OpenAsync(match, ct);
    }

    public async Task MarkLoadedAsync(Guid playerId, Guid matchId, CancellationToken ct)
    {
        await using var matchLock = await store.LockAsync(matchId, ct);
        var match = await GetRequiredAsync(matchId, ct);

        await ApplyAsync(match, match.MarkLoaded(playerId, Now), "Match finished", ct);
    }

    /// <summary>
    /// Hot path: called on every click, so the match is not locked. 
    /// The score is stored separately and incremented atomically on the storage side.
    /// </summary>
    public async Task ClickAsync(Guid playerId, Guid matchId, CancellationToken ct)
    {
        var match = await store.GetAsync(matchId, ct);
        if (match is null || !match.AcceptsClickFrom(playerId, Now))
            return;

        var interval = TimeSpan.FromMilliseconds(options.MinClickIntervalMs);
        var score = await store.TryRegisterClickAsync(matchId, playerId, interval, ct);
        if (score is null)
            return;

        await notifier.ScoreChanged(match.ActivePlayerIds, new ScoreChangedDto(matchId, playerId, score.Value));
    }

    public async Task RequestRematchAsync(Guid playerId, Guid matchId, CancellationToken ct)
    {
        await using var matchLock = await store.LockAsync(matchId, ct);
        var match = await GetRequiredAsync(matchId, ct);

        await ApplyAsync(match, match.RequestRematch(playerId), "Opponents have left the match", ct);
    }

    /// <summary>The player has left for the lobby or disconnected.</summary>
    public async Task LeaveAsync(Guid playerId, CancellationToken ct)
    {
        var matchId = await store.GetPlayerMatchAsync(playerId, ct);
        if (matchId is null)
            return;

        await store.SetPlayerMatchAsync(playerId, null, ct);

        await using var matchLock = await store.LockAsync(matchId.Value, ct);
        var match = await store.GetAsync(matchId.Value, ct);
        if (match is null)
            return;

        await ApplyAsync(match, match.Leave(playerId, Now), "Opponents have left the match", ct);
    }

    /// <summary>Called by a background service when a match reaches its deadline.</summary>
    public async Task ProcessDeadlineAsync(Guid matchId, CancellationToken ct)
    {
        await using var matchLock = await store.LockAsync(matchId, ct);

        var match = await store.GetAsync(matchId, ct);
        if (match is null)
        {
            await store.DeleteAsync(matchId, ct); // The match expired via TTL, so remove it from the deadline queue
            return;
        }

        var phaseBefore = match.Phase;
        var transition = match.OnDeadline(Now, out var dropped);

        if (dropped.Count > 0)
        {
            foreach (var playerId in dropped)
                await store.SetPlayerMatchAsync(playerId, null, ct);

            await notifier.MatchClosed(dropped, new MatchClosedDto(match.Id, "You did not load in time"));
        }

        var closeReason = phaseBefore switch
        {
            MatchPhase.Loading => "Players failed to load",
            MatchPhase.Finished => "Rematch wait time has expired",
            _ => "Match finished"
        };

        await ApplyAsync(match, transition, closeReason, ct);
    }

    private async Task ApplyAsync(Match match, MatchTransition transition, string closeReason, CancellationToken ct)
    {
        switch (transition)
        {
            case MatchTransition.Started:
                await store.SaveAsync(match, ct);
                await notifier.MatchStarted(match.ActivePlayerIds, new MatchStartedDto(
                    match.Id,
                    (int)match.Timings.Countdown.TotalMilliseconds,
                    (int)match.Timings.Round.TotalMilliseconds,
                    ToPlayers(match)));
                break;

            case MatchTransition.FinishDue:
                await FinishAsync(match, ct);
                break;

            case MatchTransition.RematchUpdated:
                await store.SaveAsync(match, ct);
                await notifier.RematchStatus(match.ActivePlayerIds, new RematchStatusDto(
                    match.Id,
                    match.Active.Where(p => p.WantsRematch).Select(p => p.PlayerId).ToList(),
                    match.ActivePlayerIds));
                break;

            case MatchTransition.RematchReady:
                var rematch = match.CreateRematch(Now);
                match.Close();
                await store.DeleteAsync(match.Id, ct);
                await OpenAsync(rematch, ct);
                break;

            case MatchTransition.Closed:
                await CloseAsync(match, closeReason, ct);
                break;

            default:
                await store.SaveAsync(match, ct);
                break;
        }
    }

    private async Task<MatchStartingDto> OpenAsync(Match match, CancellationToken ct)
    {
        await store.SaveAsync(match, ct);

        foreach (var participant in match.Participants)
            await store.SetPlayerMatchAsync(participant.PlayerId, match.Id, ct);

        var dto = new MatchStartingDto(match.Id, ToPlayers(match));
        await notifier.MatchStarting(match.ActivePlayerIds, dto);
        return dto;
    }

    private async Task FinishAsync(Match match, CancellationToken ct)
    {
        var scores = await store.GetScoresAsync(match.Id, ct);
        match.Finish(scores, Now);

        var winners = match.GetWinners().ToHashSet();
        var results = new List<PlayerResultDto>();

        // The result is recorded by the server, not the client: the score is calculated on the server, so it cannot be faked.
        foreach (var p in match.Active.OrderByDescending(p => p.Score))
        {
            var submitted = await leaderboards.SubmitAsync(options.LeaderboardKey, p.PlayerId, p.Score, ct);

            results.Add(new PlayerResultDto(
                p.PlayerId, p.Username, p.Score, winners.Contains(p.PlayerId),
                submitted.BestScore, submitted.Rank, submitted.IsNewBest));
        }

        await store.SaveAsync(match, ct);
        await notifier.MatchFinished(match.ActivePlayerIds, new MatchFinishedDto(match.Id, results));
    }

    private async Task CloseAsync(Match match, string reason, CancellationToken ct)
    {
        await store.DeleteAsync(match.Id, ct);

        foreach (var participant in match.Participants)
        {
            if (await store.GetPlayerMatchAsync(participant.PlayerId, ct) == match.Id)
                await store.SetPlayerMatchAsync(participant.PlayerId, null, ct);
        }

        var recipients = match.ActivePlayerIds;
        if (recipients.Count > 0)
            await notifier.MatchClosed(recipients, new MatchClosedDto(match.Id, reason));
    }

    private async Task<Match> GetRequiredAsync(Guid matchId, CancellationToken ct) =>
        await store.GetAsync(matchId, ct) ?? throw new NotFoundException("Match not found");

    private static IReadOnlyList<MatchPlayerDto> ToPlayers(Match match) =>
        match.Active.Select(p => new MatchPlayerDto(p.PlayerId, p.Username)).ToList();
}
