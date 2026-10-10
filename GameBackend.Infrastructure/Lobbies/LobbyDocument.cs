using GameBackend.Domain.Lobbies;

namespace GameBackend.Infrastructure.Lobbies;

internal record LobbyMemberDocument(Guid PlayerId, string Username, bool IsReady);

internal record LobbyDocument(
    Guid Id, string Name, Guid HostId, int MaxPlayers,
    LobbyState State, DateTime CreatedAt, List<LobbyMemberDocument> Members)
{
    public static LobbyDocument From(Lobby lobby) => new(
        lobby.Id, lobby.Name, lobby.HostId, lobby.MaxPlayers, lobby.State, lobby.CreatedAt,
        lobby.Members.Select(m => new LobbyMemberDocument(m.PlayerId, m.Username, m.IsReady)).ToList());

    public Lobby ToDomain() => Lobby.Restore(
        Id, Name, HostId, MaxPlayers, State, CreatedAt,
        Members.Select(m => new LobbyMember(m.PlayerId, m.Username, m.IsReady)));
}