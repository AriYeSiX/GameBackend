using GameBackend.Application.Abstractions;
using GameBackend.Application.Common;
using GameBackend.Application.Matches;
using GameBackend.Application.Players;
using GameBackend.Domain;
using GameBackend.Domain.Lobbies;

namespace GameBackend.Application.Lobbies;

public class LobbyService(
    ILobbyStore store,
    PlayerService playerService,
    MatchService matchService,
    TimeProvider timeProvider)
{
    public async Task<LobbyDto> CreateAsync(Guid playerId, string name, int maxPlayers, CancellationToken ct)
    {
        await matchService.LeaveAsync(playerId, ct); // the player came back to the lobby from a match, so leave the match

        if (await GetActiveLobbyIdAsync(playerId, ct) is not null)
            throw new DomainException("Leave your current lobby first");

        var player = await playerService.GetAsync(playerId, ct);
        var lobby = Lobby.Create(name, maxPlayers, playerId, player.Username, timeProvider.GetUtcNow().UtcDateTime);

        await store.SaveAsync(lobby, ct);
        await store.SetPlayerLobbyAsync(playerId, lobby.Id, ct);
        return lobby.ToDto();
    }

    public async Task<LobbyDto> JoinAsync(Guid playerId, Guid lobbyId, CancellationToken ct)
    {
        await matchService.LeaveAsync(playerId, ct);

        var currentLobbyId = await GetActiveLobbyIdAsync(playerId, ct);
        if (currentLobbyId is not null && currentLobbyId != lobbyId)
            throw new DomainException("Leave your current lobby first");

        var player = await playerService.GetAsync(playerId, ct);

        await using var lobbyLock = await store.LockAsync(lobbyId, ct);

        var lobby = await store.GetAsync(lobbyId, ct)
                    ?? throw new NotFoundException("Lobby not found");

        lobby.Join(playerId, player.Username);

        await store.SaveAsync(lobby, ct);
        await store.SetPlayerLobbyAsync(playerId, lobbyId, ct);
        return lobby.ToDto();
    }

    public async Task<LeaveResult?> LeaveAsync(Guid playerId, CancellationToken ct)
    {
        var lobbyId = await store.GetPlayerLobbyAsync(playerId, ct);
        if (lobbyId is null)
            return null;

        await using var lobbyLock = await store.LockAsync(lobbyId.Value, ct);
        await store.SetPlayerLobbyAsync(playerId, null, ct);

        var lobby = await store.GetAsync(lobbyId.Value, ct);
        if (lobby is null)
            return new LeaveResult(lobbyId.Value, null);

        lobby.Leave(playerId);

        if (lobby.IsEmpty)
        {
            await store.DeleteAsync(lobby.Id, ct);
            return new LeaveResult(lobby.Id, null);
        }

        await store.SaveAsync(lobby, ct);
        return new LeaveResult(lobby.Id, lobby.ToDto());
    }

    public async Task<ReadyResult> SetReadyAsync(Guid playerId, bool isReady, CancellationToken ct)
    {
        var lobbyId = await GetActiveLobbyIdAsync(playerId, ct)
                      ?? throw new DomainException("You are not in a lobby");

        await using var lobbyLock = await store.LockAsync(lobbyId, ct);

        var lobby = await store.GetAsync(lobbyId, ct)
                    ?? throw new NotFoundException("Lobby not found");

        if (!lobby.SetReady(playerId, isReady))
        {
            await store.SaveAsync(lobby, ct);
            return new ReadyResult(lobby.ToDto(), null);
        }

        await store.DeleteAsync(lobby.Id, ct);
        foreach (var member in lobby.Members)
            await store.SetPlayerLobbyAsync(member.PlayerId, null, ct);

        // The lobby has done its job: from this point on, the match takes control of the players.
        var match = await matchService.CreateAsync(
            lobby.Members.Select(m => (m.PlayerId, m.Username)).ToList(), ct);

        return new ReadyResult(lobby.ToDto(), match);
    }

    public async Task<IReadOnlyList<LobbyDto>> ListOpenAsync(CancellationToken ct)
    {
        var lobbies = await store.ListAsync(ct);

        return lobbies
            .Where(l => l.State == LobbyState.Waiting && !l.IsFull)
            .OrderBy(l => l.CreatedAt)
            .Select(l => l.ToDto())
            .ToList();
    }

    // A player's association with a lobby could persist even after the lobby itself ceased to exist
    // (i.e., its TTL expired) — in such cases, we clear that association.
    private async Task<Guid?> GetActiveLobbyIdAsync(Guid playerId, CancellationToken ct)
    {
        var lobbyId = await store.GetPlayerLobbyAsync(playerId, ct);
        if (lobbyId is null)
            return null;

        if (await store.GetAsync(lobbyId.Value, ct) is not null)
            return lobbyId;

        await store.SetPlayerLobbyAsync(playerId, null, ct);
        return null;
    }
}