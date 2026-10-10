using GameBackend.Application.Abstractions;
using GameBackend.Application.Leaderboards;
using GameBackend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace GameBackend.Infrastructure.Leaderboards;

public class RedisLeaderboardRanking(
    IConnectionMultiplexer redis,
    AppDbContext db,
    ILogger<RedisLeaderboardRanking> logger) : ILeaderboardRanking
{
    private IDatabase Redis => redis.GetDatabase();

    private static RedisKey ScoresKey(Guid id) => $"lb:{id}:scores";
    private static RedisKey ReadyKey(Guid id) => $"lb:{id}:ready";

    public async Task SetScoreAsync(Guid leaderboardId, Guid playerId, long score, CancellationToken ct)
    {
        await EnsureLoadedAsync(leaderboardId, ct);
        await Redis.SortedSetAddAsync(ScoresKey(leaderboardId), playerId.ToString(), score, SortedSetWhen.GreaterThan);
    }

    public async Task<IReadOnlyList<RankedScore>> GetTopAsync(Guid leaderboardId, int count, CancellationToken ct)
    {
        await EnsureLoadedAsync(leaderboardId, ct);

        var entries = await Redis.SortedSetRangeByRankWithScoresAsync(
            ScoresKey(leaderboardId), 0, count - 1, Order.Descending);

        return entries
            .Select(e => new RankedScore(Guid.Parse(e.Element.ToString()), (long)e.Score))
            .ToList();
    }

    public async Task<long> GetRankAsync(Guid leaderboardId, long score, CancellationToken ct)
    {
        await EnsureLoadedAsync(leaderboardId, ct);

        var higher = await Redis.SortedSetLengthAsync(
            ScoresKey(leaderboardId), score, double.PositiveInfinity, Exclude.Start);

        return higher + 1;
    }

    private async Task EnsureLoadedAsync(Guid leaderboardId, CancellationToken ct)
    {
        if (await Redis.KeyExistsAsync(ReadyKey(leaderboardId)))
            return;

        var rows = await db.ScoreEntries
            .AsNoTracking()
            .Where(e => e.LeaderboardId == leaderboardId)
            .Select(e => new { e.PlayerId, e.Score })
            .ToListAsync(ct);

        var key = ScoresKey(leaderboardId);

        if (rows.Count == 0)
        {
            await Redis.KeyDeleteAsync(key);
        }
        else
        {
            RedisKey tempKey = $"lb:{leaderboardId}:rebuild:{Guid.NewGuid():N}";

            foreach (var chunk in rows.Chunk(10_000))
            {
                await Redis.SortedSetAddAsync(tempKey,
                    chunk.Select(r => new SortedSetEntry(r.PlayerId.ToString(), r.Score)).ToArray());
            }

            await Redis.KeyRenameAsync(tempKey, key);
        }

        await Redis.StringSetAsync(ReadyKey(leaderboardId), "1");
        logger.LogInformation("Leaderboard {LeaderboardId} loaded into Redis: {Count} entries",
            leaderboardId, rows.Count);
    }
}