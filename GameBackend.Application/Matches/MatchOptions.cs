using GameBackend.Domain.Matches;

namespace GameBackend.Application.Matches;

/// <summary>Match settings from the "Matches" section in appsettings.</summary>
public class MatchOptions
{
    public const string SectionName = "Matches";

    public string LeaderboardKey { get; init; } = "classic";
    public int LoadingTimeoutMs { get; init; } = 20_000;
    public int CountdownMs { get; init; } = 3_000;
    public int RoundMs { get; init; } = 10_000;
    public int ClickGraceMs { get; init; } = 500;
    public int RematchTimeoutMs { get; init; } = 30_000;

    /// <summary>Minimum interval between clicks by a single player (protection against auto-clickers).
    /// 0 disables the limit.</summary>
    public int MinClickIntervalMs { get; init; } = 40;

    public MatchTimings ToTimings() => new(
        TimeSpan.FromMilliseconds(LoadingTimeoutMs),
        TimeSpan.FromMilliseconds(CountdownMs),
        TimeSpan.FromMilliseconds(RoundMs),
        TimeSpan.FromMilliseconds(ClickGraceMs),
        TimeSpan.FromMilliseconds(RematchTimeoutMs));
}
