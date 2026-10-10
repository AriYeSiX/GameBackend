namespace GameBackend.Domain.Entities;

public class Leaderboard
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}