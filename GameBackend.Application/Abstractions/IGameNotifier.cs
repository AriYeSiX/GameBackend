using GameBackend.Application.Matches;

namespace GameBackend.Application.Abstractions;

/// <summary>
/// Broadcasts match events to players. The Application layer knows nothing about SignalR:
/// the implementation lives in the Api layer,
/// so events can be sent from both the hub and a background service.
/// </summary>
public interface IGameNotifier
{
    Task MatchStarting(IReadOnlyList<Guid> playerIds, MatchStartingDto match);
    Task MatchStarted(IReadOnlyList<Guid> playerIds, MatchStartedDto match);
    Task ScoreChanged(IReadOnlyList<Guid> playerIds, ScoreChangedDto score);
    Task MatchFinished(IReadOnlyList<Guid> playerIds, MatchFinishedDto result);
    Task RematchStatus(IReadOnlyList<Guid> playerIds, RematchStatusDto status);
    Task MatchClosed(IReadOnlyList<Guid> playerIds, MatchClosedDto closed);
}
