using GameBackend.Application.Lobbies;

namespace GameBackend.Api.Hubs;

public interface ILobbyClient
{
    Task LobbyUpdated(LobbyDto lobby);
    Task MatchStarting(MatchStartingDto match);
}