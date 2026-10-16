using System.Text.Json;
using GameBackend.Application.Abstractions;
using GameBackend.Domain.Matches;
using GameBackend.Infrastructure.Redis;
using StackExchange.Redis;

namespace GameBackend.Infrastructure.Matches;

public class RedisMatchStore(IConnectionMultiplexer redis, TimeProvider timeProvider) : IMatchStore
{
    private static readonly TimeSpan MatchTtl = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(5);
    private static readonly RedisKey DeadlinesKey = "matches:deadlines";

    private IDatabase Db => redis.GetDatabase();

    private static RedisKey MatchKey(Guid id) => $"match:{id}";
    private static RedisKey ScoresKey(Guid id) => $"match:{id}:scores";
    private static RedisKey ThrottleKey(Guid matchId, Guid playerId) => $"match:{matchId}:throttle:{playerId}";
    private static RedisKey PlayerKey(Guid playerId) => $"player:{playerId}:match";
    private static RedisKey LockKey(Guid id) => $"lock:match:{id}";

    public async Task<Match?> GetAsync(Guid matchId, CancellationToken ct)
    {
        var json = await Db.StringGetAsync(MatchKey(matchId));
        return json.IsNullOrEmpty
            ? null
            : JsonSerializer.Deserialize<MatchDocument>(json.ToString())!.ToDomain();
    }

    public async Task SaveAsync(Match match, CancellationToken ct)
    {
        if (match.Phase == MatchPhase.Closed)
        {
            await DeleteAsync(match.Id, ct);
            return;
        }

        var json = JsonSerializer.Serialize(MatchDocument.From(match));
        await Db.StringSetAsync(MatchKey(match.Id), json, MatchTtl);

        // A queue of deadlines: a Sorted Set, where the score represents the moment at which a match needs to be advanced. 
        // A background service retrieves everything from it that is due.
        await Db.SortedSetAddAsync(DeadlinesKey, match.Id.ToString(), match.Deadline.Ticks);
    }

    public async Task DeleteAsync(Guid matchId, CancellationToken ct)
    {
        await Db.KeyDeleteAsync([MatchKey(matchId), ScoresKey(matchId)]);
        await Db.SortedSetRemoveAsync(DeadlinesKey, matchId.ToString());
    }

    public Task<IAsyncDisposable> LockAsync(Guid matchId, CancellationToken ct) =>
        RedisDistributedLock.AcquireAsync(Db, LockKey(matchId), LockTimeout, timeProvider, ct);

    public async Task<Guid?> GetPlayerMatchAsync(Guid playerId, CancellationToken ct)
    {
        var value = await Db.StringGetAsync(PlayerKey(playerId));
        return value.IsNullOrEmpty ? null : Guid.Parse(value.ToString());
    }

    public Task SetPlayerMatchAsync(Guid playerId, Guid? matchId, CancellationToken ct) =>
        matchId is null
            ? Db.KeyDeleteAsync(PlayerKey(playerId))
            : Db.StringSetAsync(PlayerKey(playerId), matchId.ToString(), MatchTtl);

    public async Task<long?> TryRegisterClickAsync(Guid matchId, Guid playerId, TimeSpan minInterval, CancellationToken ct)
    {
        // LockTake is SET NX PX: the key is created only if it does not exist and expires after minInterval.
        // If it could not be set, the previous click was too recent.
        if (minInterval > TimeSpan.Zero &&
            !await Db.LockTakeAsync(ThrottleKey(matchId, playerId), 1, minInterval))
        {
            return null;
        }

        var scoresKey = ScoresKey(matchId);
        var score = await Db.HashIncrementAsync(scoresKey, playerId.ToString());
        await Db.KeyExpireAsync(scoresKey, MatchTtl);
        return score;
    }

    public async Task<IReadOnlyDictionary<Guid, long>> GetScoresAsync(Guid matchId, CancellationToken ct)
    {
        var entries = await Db.HashGetAllAsync(ScoresKey(matchId));
        return entries.ToDictionary(e => Guid.Parse(e.Name.ToString()), e => (long)e.Value);
    }

    public async Task<IReadOnlyList<Guid>> GetDueMatchesAsync(DateTime now, CancellationToken ct)
    {
        var due = await Db.SortedSetRangeByScoreAsync(DeadlinesKey, double.NegativeInfinity, now.Ticks);
        return due.Select(v => Guid.Parse(v.ToString())).ToList();
    }
}
