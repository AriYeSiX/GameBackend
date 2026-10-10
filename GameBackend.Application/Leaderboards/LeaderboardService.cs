using GameBackend.Application.Abstractions;
using GameBackend.Application.Common;
using GameBackend.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GameBackend.Application.Leaderboards;

public class LeaderboardService(IAppDbContext db, ILeaderboardRanking ranking, TimeProvider timeProvider)
{
    public const int MaxTopCount = 100;

    public async Task<SubmitScoreResponse> SubmitAsync(string key, Guid playerId, long score, CancellationToken ct)
    {
        var board = await GetBoardAsync(key, ct);

        var entry = await db.ScoreEntries
            .FirstOrDefaultAsync(e => e.LeaderboardId == board.Id && e.PlayerId == playerId, ct);

        var isNewBest = entry is null || score > entry.Score;

        if (isNewBest)
        {
            if (entry is null)
            {
                entry = new ScoreEntry { Id = Guid.NewGuid(), LeaderboardId = board.Id, PlayerId = playerId };
                db.ScoreEntries.Add(entry);
            }

            entry.Score = score;
            entry.AchievedAt = timeProvider.GetUtcNow().UtcDateTime;
            await db.SaveChangesAsync(ct);
            await ranking.SetScoreAsync(board.Id, playerId, score, ct);
        }

        var rank = await ranking.GetRankAsync(board.Id, entry!.Score, ct);
        return new SubmitScoreResponse(entry.Score, rank, isNewBest);
    }

    public async Task<IReadOnlyList<LeaderboardEntry>> GetTopAsync(string key, int count, CancellationToken ct)
    {
        if (count is < 1 or > MaxTopCount)
            throw new ValidationFailedException($"count должен быть от 1 до {MaxTopCount}");

        var board = await GetBoardAsync(key, ct);
        var top = await ranking.GetTopAsync(board.Id, count, ct);

        var ids = top.Select(t => t.PlayerId).ToList();
        var names = await db.Players
            .Where(p => ids.Contains(p.Id))
            .Select(p => new { p.Id, p.Username })
            .ToDictionaryAsync(p => p.Id, p => p.Username, ct);

        var result = new List<LeaderboardEntry>(top.Count);
        long rank = 0;

        for (var i = 0; i < top.Count; i++)
        {
            if (i == 0 || top[i].Score != top[i - 1].Score)
                rank = i + 1;

            result.Add(new LeaderboardEntry(
                rank, top[i].PlayerId, names.GetValueOrDefault(top[i].PlayerId, "?"), top[i].Score));
        }

        return result;
    }

    public async Task<PlayerRankResponse> GetPlayerRankAsync(string key, Guid playerId, CancellationToken ct)
    {
        var board = await GetBoardAsync(key, ct);

        var entry = await db.ScoreEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.LeaderboardId == board.Id && e.PlayerId == playerId, ct)
            ?? throw new NotFoundException("У игрока нет результата в этой таблице");

        var rank = await ranking.GetRankAsync(board.Id, entry.Score, ct);
        return new PlayerRankResponse(rank, entry.Score);
    }

    private async Task<Leaderboard> GetBoardAsync(string key, CancellationToken ct) =>
        await db.Leaderboards.AsNoTracking().FirstOrDefaultAsync(b => b.Key == key, ct)
        ?? throw new NotFoundException($"Таблица '{key}' не найдена");
}