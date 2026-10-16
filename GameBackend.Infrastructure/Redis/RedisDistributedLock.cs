using StackExchange.Redis;

namespace GameBackend.Infrastructure.Redis;

/// <summary>
/// Distributed lock on top of Redis SET NX PX, shared by the lobby and match stores.
/// The key expires on its own if the process dies while holding the lock,
/// and only the owner (identified by a random token) can release it.
/// </summary>
internal static class RedisDistributedLock
{
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(10);

    public static async Task<IAsyncDisposable> AcquireAsync(
        IDatabase db, RedisKey key, TimeSpan timeout, TimeProvider timeProvider, CancellationToken ct)
    {
        var token = Guid.NewGuid().ToString();
        var deadline = timeProvider.GetUtcNow() + timeout;

        while (!await db.LockTakeAsync(key, token, timeout))
        {
            if (timeProvider.GetUtcNow() > deadline)
                throw new TimeoutException($"Failed to acquire lock '{key}'");

            await Task.Delay(RetryDelay, timeProvider, ct);
        }

        return new Releaser(db, key, token);
    }

    private sealed class Releaser(IDatabase db, RedisKey key, RedisValue token) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync() => await db.LockReleaseAsync(key, token);
    }
}
