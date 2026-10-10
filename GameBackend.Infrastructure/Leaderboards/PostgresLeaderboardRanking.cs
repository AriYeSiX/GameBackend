using GameBackend.Application.Abstractions;
using GameBackend.Application.Leaderboards;
using GameBackend.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GameBackend.Infrastructure.Leaderboards;

public class PostgresLeaderboardRanking(AppDbContext db) : ILeaderboardRanking
{
    public Task SetScoreAsync(Guid leaderboardId, Guid playerId, long score, CancellationToken ct) =>
        Task.CompletedTask; 

    public async Task<IReadOnlyList<RankedScore>> GetTopAsync(Guid leaderboardId, int count, CancellationToken ct) =>
        await db.ScoreEntries
            .AsNoTracking()
            .Where(e => e.LeaderboardId == leaderboardId)
            .OrderByDescending(e => e.Score)
            .ThenBy(e => e.AchievedAt)
            .Take(count)
            .Select(e => new RankedScore(e.PlayerId, e.Score))
            .ToListAsync(ct);

    public async Task<long> GetRankAsync(Guid leaderboardId, long score, CancellationToken ct) =>
        await db.ScoreEntries.LongCountAsync(e => e.LeaderboardId == leaderboardId && e.Score > score, ct) + 1;
}