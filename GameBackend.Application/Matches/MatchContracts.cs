namespace GameBackend.Application.Matches;

public record MatchPlayerDto(Guid PlayerId, string Username);

/// <summary>Match created: clients must proceed to the loading screen.</summary>
public record MatchStartingDto(Guid MatchId, IReadOnlyList<MatchPlayerDto> Players);

/// <summary>Everyone has loaded: after a CountdownMs countdown, a DurationMs round begins.</summary>
/// <remarks>
/// We transmit durations rather than absolute time: client and server clocks may differ,
/// so the client counts time from the moment it receives the event.
/// </remarks>
public record MatchStartedDto(Guid MatchId, int CountdownMs, int DurationMs, IReadOnlyList<MatchPlayerDto> Players);

public record ScoreChangedDto(Guid MatchId, Guid PlayerId, long Score);

public record PlayerResultDto(
    Guid PlayerId, string Username, long Score, bool IsWinner, long BestScore, long Rank, bool IsNewBest);

public record MatchFinishedDto(Guid MatchId, IReadOnlyList<PlayerResultDto> Results);

public record RematchStatusDto(Guid MatchId, IReadOnlyList<Guid> ReadyPlayerIds, IReadOnlyList<Guid> ActivePlayerIds);

public record MatchClosedDto(Guid MatchId, string Reason);
