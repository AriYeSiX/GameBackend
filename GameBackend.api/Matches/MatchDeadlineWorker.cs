using GameBackend.Application.Abstractions;
using GameBackend.Application.Matches;

namespace GameBackend.Api.Matches;

/// <summary>
/// Advances matches over time: loading timeout, end of round, rematch timeout.
/// Every PollInterval it fetches matches whose deadline has passed from Redis.
/// Safe with multiple API instances: each match is processed under a lock,
/// and MatchService re-checks the deadline after acquiring it.
/// </summary>
public class MatchDeadlineWorker(
    IServiceScopeFactory scopeFactory,
    IMatchStore store,
    TimeProvider timeProvider,
    ILogger<MatchDeadlineWorker> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(100);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(PollInterval, timeProvider);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            IReadOnlyList<Guid> due;

            try
            {
                due = await store.GetDueMatchesAsync(timeProvider.GetUtcNow().UtcDateTime, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Failed to fetch matches with an expired deadline");
                continue;
            }

            foreach (var matchId in due)
            {
                try
                {
                    // MatchService is scoped (it uses DbContext through LeaderboardService),
                    // so each match gets its own scope
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var matches = scope.ServiceProvider.GetRequiredService<MatchService>();
                    await matches.ProcessDeadlineAsync(matchId, stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    logger.LogError(ex, "Failed to process the deadline of match {MatchId}", matchId);
                }
            }
        }
    }
}
