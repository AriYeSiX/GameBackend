using GameBackend.Application.Lobbies;
using GameBackend.Application.Matches;

namespace GameBackend.Api.Hubs;

/// <summary>Events the server sends to clients.</summary>
public interface IGameClient
{
    Task LobbyUpdated(LobbyDto lobby);
    Task MatchStarting(MatchStartingDto match);
    Task MatchStarted(MatchStartedDto match);
    Task ScoreChanged(ScoreChangedDto score);
    Task MatchFinished(MatchFinishedDto result);
    Task RematchStatus(RematchStatusDto status);
    Task MatchClosed(MatchClosedDto closed);
}
