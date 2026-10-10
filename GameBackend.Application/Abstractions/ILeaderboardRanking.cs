using GameBackend.Application.Leaderboards;

namespace GameBackend.Application.Abstractions;

public interface ILeaderboardRanking
{
    Task SetScoreAsync(Guid leaderboardId, Guid playerId, long score, CancellationToken ct);
    Task<IReadOnlyList<RankedScore>> GetTopAsync(Guid leaderboardId, int count, CancellationToken ct);
    Task<long> GetRankAsync(Guid leaderboardId, long score, CancellationToken ct);
}