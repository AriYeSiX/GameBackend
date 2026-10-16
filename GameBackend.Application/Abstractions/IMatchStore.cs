using GameBackend.Domain.Matches;

namespace GameBackend.Application.Abstractions;

public interface IMatchStore
{
    Task<Match?> GetAsync(Guid matchId, CancellationToken ct);

    /// <summary>Saves the match and adds it to the deadline queue (a closed match is removed from the queue).</summary>
    Task SaveAsync(Match match, CancellationToken ct);

    Task DeleteAsync(Guid matchId, CancellationToken ct);

    Task<IAsyncDisposable> LockAsync(Guid matchId, CancellationToken ct);

    Task<Guid?> GetPlayerMatchAsync(Guid playerId, CancellationToken ct);
    Task SetPlayerMatchAsync(Guid playerId, Guid? matchId, CancellationToken ct);

    /// <summary>
    /// Registers a click without locking the match. Returns the player's new score,
    /// or null if the click occurred less than <paramref name="minInterval"/> after the previous one.
    /// </summary>
    Task<long?> TryRegisterClickAsync(Guid matchId, Guid playerId, TimeSpan minInterval, CancellationToken ct);

    Task<IReadOnlyDictionary<Guid, long>> GetScoresAsync(Guid matchId, CancellationToken ct);

    /// <summary>Matches for which the deadline has passed.</summary>
    Task<IReadOnlyList<Guid>> GetDueMatchesAsync(DateTime now, CancellationToken ct);
}
