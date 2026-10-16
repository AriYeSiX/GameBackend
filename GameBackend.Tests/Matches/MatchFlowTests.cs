using System.Net.Http.Json;
using System.Threading.Channels;
using GameBackend.Application.Lobbies;
using GameBackend.Application.Matches;
using GameBackend.Application.Players;
using GameBackend.Tests.Infrastructure;
using Microsoft.AspNetCore.SignalR.Client;

namespace GameBackend.Tests.Matches;

[TestFixture]
public class MatchFlowTests
{
    private static readonly TimeSpan EventTimeout = TimeSpan.FromSeconds(10);
    private static readonly MatchOptions Timings = ApiFactory.TestMatchOptions;

    [Test]
    public async Task FullMatch_ServerCountsClicks_AndMarksWinner()
    {
        await using var host = await TestPlayer.ConnectAsync();
        await using var guest = await TestPlayer.ConnectAsync();

        var matchId = await StartMatchAsync(host, guest);
        await WaitForRoundAsync();

        await host.ClickAsync(matchId, times: 3);
        await guest.ClickAsync(matchId, times: 1);

        var result = await host.Finished.NextAsync();
        var hostResult = result.Results.Single(r => r.PlayerId == host.Id);
        var guestResult = result.Results.Single(r => r.PlayerId == guest.Id);

        Assert.Multiple(() =>
        {
            Assert.That(hostResult.Score, Is.EqualTo(3));
            Assert.That(guestResult.Score, Is.EqualTo(1));
            Assert.That(hostResult.IsWinner, Is.True);
            Assert.That(guestResult.IsWinner, Is.False);
            Assert.That(hostResult.BestScore, Is.EqualTo(3)); // the result has been recorded on the leaderboard
        });
    }

    [Test]
    public async Task Clicks_FasterThanMinInterval_AreIgnored()
    {
        await using var host = await TestPlayer.ConnectAsync();
        await using var guest = await TestPlayer.ConnectAsync();

        var matchId = await StartMatchAsync(host, guest);
        await WaitForRoundAsync();

        // Five consecutive clicks without a pause — like an auto-clicker
        for (var i = 0; i < 5; i++)
            await host.Connection.SendAsync("Click", matchId);

        var result = await host.Finished.NextAsync();

        Assert.That(result.Results.Single(r => r.PlayerId == host.Id).Score, Is.LessThan(5));
    }

    [Test]
    public async Task Rematch_WhenAllAgree_StartsNewMatch()
    {
        await using var host = await TestPlayer.ConnectAsync();
        await using var guest = await TestPlayer.ConnectAsync();

        var matchId = await StartMatchAsync(host, guest);
        await host.Finished.NextAsync();
        await guest.Finished.NextAsync();

        await host.Connection.InvokeAsync("RequestRematch", matchId);
        await guest.Connection.InvokeAsync("RequestRematch", matchId);

        var hostRematch = await host.Starting.NextAsync();
        var guestRematch = await guest.Starting.NextAsync();

        Assert.Multiple(() =>
        {
            Assert.That(hostRematch.MatchId, Is.Not.EqualTo(matchId));
            Assert.That(hostRematch.MatchId, Is.EqualTo(guestRematch.MatchId));
        });
    }

    [Test]
    public async Task OpponentLeavesAfterFinish_RemainingPlayerGetsMatchClosed()
    {
        await using var host = await TestPlayer.ConnectAsync();
        await using var guest = await TestPlayer.ConnectAsync();

        var matchId = await StartMatchAsync(host, guest);
        await host.Finished.NextAsync();

        await guest.Connection.InvokeAsync("LeaveMatch");
        var closed = await host.Closed.NextAsync();

        Assert.That(closed.MatchId, Is.EqualTo(matchId));
    }

    [Test]
    public async Task PlayerNeverLoads_MatchStartsWithoutHim_AfterTimeout()
    {
        await using var host = await TestPlayer.ConnectAsync();
        await using var guest = await TestPlayer.ConnectAsync();

        var starting = await CreateMatchFromLobbyAsync(host, guest);
        await host.Connection.InvokeAsync("MatchLoaded", starting.MatchId); // the guest got stuck during loading

        var started = await host.Started.NextAsync();
        var guestClosed = await guest.Closed.NextAsync();

        Assert.Multiple(() =>
        {
            Assert.That(started.Players.Select(p => p.PlayerId), Is.EqualTo(new[] { host.Id }));
            Assert.That(guestClosed.MatchId, Is.EqualTo(starting.MatchId));
        });
    }

    private static async Task<MatchStartingDto> CreateMatchFromLobbyAsync(TestPlayer host, TestPlayer guest)
    {
        var lobby = await host.Connection.InvokeAsync<LobbyDto>("CreateLobby", "Match test", 2);
        await guest.Connection.InvokeAsync<LobbyDto>("JoinLobby", lobby.Id);

        await host.Connection.InvokeAsync("SetReady", true);
        await guest.Connection.InvokeAsync("SetReady", true);

        var starting = await host.Starting.NextAsync();
        await guest.Starting.NextAsync();
        return starting;
    }

    private static async Task<Guid> StartMatchAsync(TestPlayer host, TestPlayer guest)
    {
        var starting = await CreateMatchFromLobbyAsync(host, guest);

        await host.Connection.InvokeAsync("MatchLoaded", starting.MatchId);
        await guest.Connection.InvokeAsync("MatchLoaded", starting.MatchId);

        await host.Started.NextAsync();
        await guest.Started.NextAsync();
        return starting.MatchId;
    }

    private static Task WaitForRoundAsync() => Task.Delay(Timings.CountdownMs + 50);

    /// <summary>A player with a connection to a hub and queues of incoming events.</summary>
    private sealed class TestPlayer : IAsyncDisposable
    {
        private TestPlayer(HubConnection connection, Guid id)
        {
            Connection = connection;
            Id = id;
            Starting = new EventQueue<MatchStartingDto>(connection, "MatchStarting");
            Started = new EventQueue<MatchStartedDto>(connection, "MatchStarted");
            Finished = new EventQueue<MatchFinishedDto>(connection, "MatchFinished");
            Closed = new EventQueue<MatchClosedDto>(connection, "MatchClosed");
        }

        public HubConnection Connection { get; }
        public Guid Id { get; }
        public EventQueue<MatchStartingDto> Starting { get; }
        public EventQueue<MatchStartedDto> Started { get; }
        public EventQueue<MatchFinishedDto> Finished { get; }
        public EventQueue<MatchClosedDto> Closed { get; }

        public static async Task<TestPlayer> ConnectAsync()
        {
            var (client, tokens) = await TestEnvironment.Factory.CreateAuthenticatedClientAsync();
            var me = (await client.GetFromJsonAsync<PlayerResponse>("/api/players/me"))!;

            var connection = TestEnvironment.Factory.CreateGameConnection(tokens.AccessToken);
            var player = new TestPlayer(connection, me.Id); // subscriptions before StartAsync, so as not to miss any events
            await connection.StartAsync();
            return player;
        }

        public async Task ClickAsync(Guid matchId, int times)
        {
            for (var i = 0; i < times; i++)
            {
                await Connection.SendAsync("Click", matchId);
                await Task.Delay(Timings.MinClickIntervalMs + 30);
            }
        }

        public ValueTask DisposeAsync() => Connection.DisposeAsync();
    }

    private sealed class EventQueue<T>
    {
        private readonly Channel<T> _channel = Channel.CreateUnbounded<T>();

        public EventQueue(HubConnection connection, string methodName) =>
            connection.On<T>(methodName, item => _channel.Writer.TryWrite(item));

        public async Task<T> NextAsync() =>
            await _channel.Reader.ReadAsync().AsTask().WaitAsync(EventTimeout);
    }
}
