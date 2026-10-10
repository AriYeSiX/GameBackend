using System.Text.Json;
using GameBackend.Application.Abstractions;
using GameBackend.Domain.Lobbies;
using StackExchange.Redis;

namespace GameBackend.Infrastructure.Lobbies;

public class RedisLobbyStore(IConnectionMultiplexer redis) : ILobbyStore
{
    private static readonly TimeSpan LobbyTtl = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(5);
    private static readonly RedisKey LobbyIndexKey = "lobbies:index";

    private IDatabase Db => redis.GetDatabase();

    private static RedisKey LobbyKey(Guid id) => $"lobby:{id}";
    private static RedisKey PlayerKey(Guid id) => $"player:{id}:lobby";
    private static RedisKey LockKey(Guid id) => $"lock:lobby:{id}";

    public async Task<Lobby?> GetAsync(Guid lobbyId, CancellationToken ct)
    {
        var json = await Db.StringGetAsync(LobbyKey(lobbyId));
        return json.IsNullOrEmpty
            ? null
            : JsonSerializer.Deserialize<LobbyDocument>(json.ToString())!.ToDomain();
    }

    public async Task SaveAsync(Lobby lobby, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(LobbyDocument.From(lobby));
        await Db.StringSetAsync(LobbyKey(lobby.Id), json, LobbyTtl);
        await Db.SetAddAsync(LobbyIndexKey, lobby.Id.ToString());
    }

    public async Task DeleteAsync(Guid lobbyId, CancellationToken ct)
    {
        await Db.KeyDeleteAsync(LobbyKey(lobbyId));
        await Db.SetRemoveAsync(LobbyIndexKey, lobbyId.ToString());
    }

    public async Task<IReadOnlyList<Lobby>> ListAsync(CancellationToken ct)
    {
        var ids = await Db.SetMembersAsync(LobbyIndexKey);
        var result = new List<Lobby>(ids.Length);

        foreach (var id in ids)
        {
            var lobby = await GetAsync(Guid.Parse(id.ToString()), ct);

            if (lobby is null)
                await Db.SetRemoveAsync(LobbyIndexKey, id); // лобби истекло по TTL — чистим индекс
            else
                result.Add(lobby);
        }

        return result;
    }

    public async Task<Guid?> GetPlayerLobbyAsync(Guid playerId, CancellationToken ct)
    {
        var value = await Db.StringGetAsync(PlayerKey(playerId));
        return value.IsNullOrEmpty ? null : Guid.Parse(value.ToString());
    }

    public Task SetPlayerLobbyAsync(Guid playerId, Guid? lobbyId, CancellationToken ct) =>
        lobbyId is null
            ? Db.KeyDeleteAsync(PlayerKey(playerId))
            : Db.StringSetAsync(PlayerKey(playerId), lobbyId.ToString(), LobbyTtl);

    public async Task<IAsyncDisposable> LockAsync(Guid lobbyId, CancellationToken ct)
    {
        var key = LockKey(lobbyId);
        var token = Guid.NewGuid().ToString();
        var deadline = DateTime.UtcNow + LockTimeout;

        while (!await Db.LockTakeAsync(key, token, LockTimeout))
        {
            if (DateTime.UtcNow > deadline)
                throw new TimeoutException($"Не удалось заблокировать лобби {lobbyId}");

            await Task.Delay(20, ct);
        }

        return new RedisLock(Db, key, token);
    }

    private sealed class RedisLock(IDatabase db, RedisKey key, RedisValue token) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync() => await db.LockReleaseAsync(key, token);
    }
}