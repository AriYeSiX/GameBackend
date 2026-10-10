namespace GameBackend.Domain.Entities;

public class ScoreEntry
{
    public Guid Id { get; set; }
    public Guid LeaderboardId { get; set; }
    public Guid PlayerId { get; set; }
    public long Score { get; set; }
    public DateTime AchievedAt { get; set; }
}