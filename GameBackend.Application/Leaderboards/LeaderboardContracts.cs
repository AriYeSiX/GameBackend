using System.ComponentModel.DataAnnotations;

namespace GameBackend.Application.Leaderboards;

public record SubmitScoreRequest([Range(0, 1_000_000_000_000)] long Score);

public record SubmitScoreResponse(long BestScore, long Rank, bool IsNewBest);

public record LeaderboardEntry(long Rank, Guid PlayerId, string Username, long Score);

public record PlayerRankResponse(long Rank, long Score);

public record RankedScore(Guid PlayerId, long Score);