using GameBackend.Application.Abstractions;
using GameBackend.Application.Common;
using GameBackend.Application.Players;
using GameBackend.Domain;
using GameBackend.Domain.Lobbies;

namespace GameBackend.Application.Lobbies;

public class LobbyService(ILobbyStore store, PlayerService playerService, TimeProvider timeProvider)
{
    public async Task<LobbyDto> CreateAsync(Guid playerId, string name, int maxPlayers, CancellationToken ct)
    {
        if (await GetActiveLobbyIdAsync(playerId, ct) is not null)
            throw new DomainException("Сначала покиньте текущее лобби");

        var player = await playerService.GetAsync(playerId, ct);
        var lobby = Lobby.Create(name, maxPlayers, playerId, player.Username, timeProvider.GetUtcNow().UtcDateTime);

        await store.SaveAsync(lobby, ct);
        await store.SetPlayerLobbyAsync(playerId, lobby.Id, ct);
        return lobby.ToDto();
    }

    public async Task<LobbyDto> JoinAsync(Guid playerId, Guid lobbyId, CancellationToken ct)
    {
        var currentLobbyId = await GetActiveLobbyIdAsync(playerId, ct);
        if (currentLobbyId is not null && currentLobbyId != lobbyId)
            throw new DomainException("Сначала покиньте текущее лобби");

        var player = await playerService.GetAsync(playerId, ct);

        await using var lobbyLock = await store.LockAsync(lobbyId, ct);

        var lobby = await store.GetAsync(lobbyId, ct)
                    ?? throw new NotFoundException("Лобби не найдено");

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
                      ?? throw new DomainException("Вы не состоите в лобби");

        await using var lobbyLock = await store.LockAsync(lobbyId, ct);

        var lobby = await store.GetAsync(lobbyId, ct)
                    ?? throw new NotFoundException("Лобби не найдено");

        if (!lobby.SetReady(playerId, isReady))
        {
            await store.SaveAsync(lobby, ct);
            return new ReadyResult(lobby.ToDto(), null);
        }

        var match = new MatchStartingDto(lobby.Id, Guid.NewGuid(), lobby.Members.Select(m => m.PlayerId).ToList());

        await store.DeleteAsync(lobby.Id, ct);
        foreach (var member in lobby.Members)
            await store.SetPlayerLobbyAsync(member.PlayerId, null, ct);

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

    // Привязка игрока к лобби могла пережить само лобби (истёк TTL) — тогда сбрасываем её
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