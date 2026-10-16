using GameBackend.Api.Hubs;
using GameBackend.Application.Abstractions;
using GameBackend.Application.Matches;
using Microsoft.AspNetCore.SignalR;

namespace GameBackend.Api.Matches;

public class SignalRGameNotifier(IHubContext<GameHub, IGameClient> hub) : IGameNotifier
{
    public Task MatchStarting(IReadOnlyList<Guid> playerIds, MatchStartingDto match) => To(playerIds).MatchStarting(match);
    public Task MatchStarted(IReadOnlyList<Guid> playerIds, MatchStartedDto match) => To(playerIds).MatchStarted(match);
    public Task ScoreChanged(IReadOnlyList<Guid> playerIds, ScoreChangedDto score) => To(playerIds).ScoreChanged(score);
    public Task MatchFinished(IReadOnlyList<Guid> playerIds, MatchFinishedDto result) => To(playerIds).MatchFinished(result);
    public Task RematchStatus(IReadOnlyList<Guid> playerIds, RematchStatusDto status) => To(playerIds).RematchStatus(status);
    public Task MatchClosed(IReadOnlyList<Guid> playerIds, MatchClosedDto closed) => To(playerIds).MatchClosed(closed);

    private IGameClient To(IReadOnlyList<Guid> playerIds) =>
        hub.Clients.Users(playerIds.Select(id => id.ToString()).ToList());
}
