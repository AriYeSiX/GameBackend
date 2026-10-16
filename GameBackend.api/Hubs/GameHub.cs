using GameBackend.Api.Extensions;
using GameBackend.Application.Lobbies;
using GameBackend.Application.Matches;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace GameBackend.Api.Hubs;

/// <summary>
/// Single real-time entry point for the client: lobby and match.
/// The hub is thin: all logic lives in LobbyService and MatchService.
/// </summary>
[Authorize]
public class GameHub(LobbyService lobbyService, MatchService matchService) : Hub<IGameClient>
{
    private Guid PlayerId => Context.User!.GetPlayerId();

    private static string LobbyGroup(Guid lobbyId) => $"lobby:{lobbyId}";

    // ---------- Lobby ----------

    public async Task<LobbyDto> CreateLobby(string name, int maxPlayers)
    {
        var lobby = await lobbyService.CreateAsync(PlayerId, name, maxPlayers, Context.ConnectionAborted);
        await Groups.AddToGroupAsync(Context.ConnectionId, LobbyGroup(lobby.Id));
        return lobby;
    }

    public async Task<LobbyDto> JoinLobby(Guid lobbyId)
    {
        var lobby = await lobbyService.JoinAsync(PlayerId, lobbyId, Context.ConnectionAborted);
        await Groups.AddToGroupAsync(Context.ConnectionId, LobbyGroup(lobby.Id));
        await Clients.Group(LobbyGroup(lobby.Id)).LobbyUpdated(lobby);
        return lobby;
    }

    public Task LeaveLobby() => LeaveCurrentLobbyAsync(Context.ConnectionAborted);

    public async Task SetReady(bool isReady)
    {
        var result = await lobbyService.SetReadyAsync(PlayerId, isReady, Context.ConnectionAborted);

        // If everyone is ready, MatchService has already broadcast MatchStarting
        if (result.Match is null)
            await Clients.Group(LobbyGroup(result.Lobby.Id)).LobbyUpdated(result.Lobby);
    }

    // ---------- Match ----------

    /// <summary>The client has loaded the gameplay scene and is ready to start.</summary>
    public Task MatchLoaded(Guid matchId) =>
        matchService.MarkLoadedAsync(PlayerId, matchId, Context.ConnectionAborted);

    /// <summary>A button click. The client calls it via Send without waiting for a response.</summary>
    public Task Click(Guid matchId) =>
        matchService.ClickAsync(PlayerId, matchId, Context.ConnectionAborted);

    public Task RequestRematch(Guid matchId) =>
        matchService.RequestRematchAsync(PlayerId, matchId, Context.ConnectionAborted);

    public Task LeaveMatch() =>
        matchService.LeaveAsync(PlayerId, Context.ConnectionAborted);

    // ---------- Connection ----------

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await LeaveCurrentLobbyAsync(CancellationToken.None);
        await matchService.LeaveAsync(PlayerId, CancellationToken.None);
        await base.OnDisconnectedAsync(exception);
    }

    private async Task LeaveCurrentLobbyAsync(CancellationToken ct)
    {
        var result = await lobbyService.LeaveAsync(PlayerId, ct);
        if (result is null)
            return;

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, LobbyGroup(result.LobbyId));

        if (result.Lobby is not null)
            await Clients.Group(LobbyGroup(result.LobbyId)).LobbyUpdated(result.Lobby);
    }
}
