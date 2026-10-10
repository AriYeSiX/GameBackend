using GameBackend.Domain.Lobbies;

namespace GameBackend.Application.Lobbies;

public record LobbyMemberDto(Guid PlayerId, string Username, bool IsReady);

public record LobbyDto(
    Guid Id, string Name, Guid HostId, int MaxPlayers, string State, IReadOnlyList<LobbyMemberDto> Members);

public record MatchStartingDto(Guid LobbyId, Guid MatchId, IReadOnlyList<Guid> PlayerIds);

public record LeaveResult(Guid LobbyId, LobbyDto? Lobby);

public record ReadyResult(LobbyDto Lobby, MatchStartingDto? Match);

public static class LobbyMappings
{
    public static LobbyDto ToDto(this Lobby lobby) => new(
        lobby.Id, lobby.Name, lobby.HostId, lobby.MaxPlayers, lobby.State.ToString(),
        lobby.Members.Select(m => new LobbyMemberDto(m.PlayerId, m.Username, m.IsReady)).ToList());
}