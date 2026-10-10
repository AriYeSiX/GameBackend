using GameBackend.Application.Lobbies;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;

namespace GameBackend.Tests.Lobbies;

[TestFixture]
public class LobbyHubTests
{
    private static readonly TimeSpan EventTimeout = TimeSpan.FromSeconds(5);

    private static async Task<HubConnection> ConnectNewPlayerAsync()
    {
        var (_, tokens) = await TestEnvironment.Factory.CreateAuthenticatedClientAsync();
        var connection = TestEnvironment.Factory.CreateLobbyConnection(tokens.AccessToken);
        await connection.StartAsync();
        return connection;
    }

    [Test]
    public async Task AllPlayersReady_MatchStartsForEveryone()
    {
        await using var host = await ConnectNewPlayerAsync();
        await using var guest = await ConnectNewPlayerAsync();

        var hostMatch = new TaskCompletionSource<MatchStartingDto>();
        var guestMatch = new TaskCompletionSource<MatchStartingDto>();
        host.On<MatchStartingDto>("MatchStarting", m => hostMatch.TrySetResult(m));
        guest.On<MatchStartingDto>("MatchStarting", m => guestMatch.TrySetResult(m));

        var lobby = await host.InvokeAsync<LobbyDto>("CreateLobby", "Test lobby", 2);
        await guest.InvokeAsync<LobbyDto>("JoinLobby", lobby.Id);

        await host.InvokeAsync("SetReady", true);
        await guest.InvokeAsync("SetReady", true);

        var hostResult = await hostMatch.Task.WaitAsync(EventTimeout);
        var guestResult = await guestMatch.Task.WaitAsync(EventTimeout);

        Assert.Multiple(() =>
        {
            Assert.That(hostResult.MatchId, Is.EqualTo(guestResult.MatchId));
            Assert.That(hostResult.PlayerIds, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public async Task Join_FullLobby_ThrowsHubException()
    {
        await using var host = await ConnectNewPlayerAsync();
        await using var guest = await ConnectNewPlayerAsync();
        await using var extra = await ConnectNewPlayerAsync();

        var lobby = await host.InvokeAsync<LobbyDto>("CreateLobby", "Small lobby", 2);
        await guest.InvokeAsync<LobbyDto>("JoinLobby", lobby.Id);

        var ex = Assert.ThrowsAsync<HubException>(() => extra.InvokeAsync<LobbyDto>("JoinLobby", lobby.Id));

        Assert.That(ex!.Message, Does.Contain("заполнено"));
    }

    [Test]
    public async Task GuestDisconnects_HostReceivesUpdatedLobby()
    {
        await using var host = await ConnectNewPlayerAsync();
        var guest = await ConnectNewPlayerAsync();

        var guestLeft = new TaskCompletionSource<LobbyDto>();
        host.On<LobbyDto>("LobbyUpdated", l =>
        {
            if (l.Members.Count == 1)
                guestLeft.TrySetResult(l);
        });

        var lobby = await host.InvokeAsync<LobbyDto>("CreateLobby", "Disconnect test", 4);
        await guest.InvokeAsync<LobbyDto>("JoinLobby", lobby.Id);
        await guest.DisposeAsync();

        var updated = await guestLeft.Task.WaitAsync(EventTimeout);

        Assert.That(updated.Members, Has.Count.EqualTo(1));
    }
}