using GameBackend.Domain.Lobbies;

namespace GameBackend.Application.Abstractions;

public interface ILobbyStore
{
    Task<Lobby?> GetAsync(Guid lobbyId, CancellationToken ct);
    Task SaveAsync(Lobby lobby, CancellationToken ct);
    Task DeleteAsync(Guid lobbyId, CancellationToken ct);
    Task<IReadOnlyList<Lobby>> ListAsync(CancellationToken ct);

    Task<Guid?> GetPlayerLobbyAsync(Guid playerId, CancellationToken ct);
    Task SetPlayerLobbyAsync(Guid playerId, Guid? lobbyId, CancellationToken ct);

    Task<IAsyncDisposable> LockAsync(Guid lobbyId, CancellationToken ct);
}