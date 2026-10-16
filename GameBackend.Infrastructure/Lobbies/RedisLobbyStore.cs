using System.Text.Json;
using GameBackend.Application.Abstractions;
using GameBackend.Domain.Lobbies;
using GameBackend.Infrastructure.Redis;
using StackExchange.Redis;

namespace GameBackend.Infrastructure.Lobbies;

public class RedisLobbyStore(IConnectionMultiplexer redis, TimeProvider timeProvider) : ILobbyStore
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
        return json.IsNullOrEmpty ? null : Deserialize(json);
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
        if (ids.Length == 0)
            return [];

        // One MGET for all lobbies instead of a round trip per lobby
        var keys = ids.Select(id => LobbyKey(Guid.Parse(id.ToString()))).ToArray();
        var values = await Db.StringGetAsync(keys);

        var result = new List<Lobby>(ids.Length);
        var expired = new List<RedisValue>();

        for (var i = 0; i < ids.Length; i++)
        {
            if (values[i].IsNullOrEmpty)
                expired.Add(ids[i]); // the lobby expired via TTL, so clean up the index
            else
                result.Add(Deserialize(values[i]));
        }

        if (expired.Count > 0)
            await Db.SetRemoveAsync(LobbyIndexKey, expired.ToArray());

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

    public Task<IAsyncDisposable> LockAsync(Guid lobbyId, CancellationToken ct) =>
        RedisDistributedLock.AcquireAsync(Db, LockKey(lobbyId), LockTimeout, timeProvider, ct);

    private static Lobby Deserialize(RedisValue json) =>
        JsonSerializer.Deserialize<LobbyDocument>(json.ToString())!.ToDomain();
}