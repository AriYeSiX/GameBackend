using GameBackend.Api.Extensions;
using GameBackend.Application.Lobbies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace GameBackend.Api.Hubs;

[Authorize]
public class LobbyHub(LobbyService lobbyService) : Hub<ILobbyClient>
{
    private Guid PlayerId => Context.User!.GetPlayerId();

    private static string GroupName(Guid lobbyId) => $"lobby:{lobbyId}";

    public async Task<LobbyDto> CreateLobby(string name, int maxPlayers)
    {
        var lobby = await lobbyService.CreateAsync(PlayerId, name, maxPlayers, Context.ConnectionAborted);
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(lobby.Id));
        return lobby;
    }

    public async Task<LobbyDto> JoinLobby(Guid lobbyId)
    {
        var lobby = await lobbyService.JoinAsync(PlayerId, lobbyId, Context.ConnectionAborted);
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(lobby.Id));
        await Clients.Group(GroupName(lobby.Id)).LobbyUpdated(lobby);
        return lobby;
    }

    public Task LeaveLobby() => LeaveCurrentLobbyAsync(Context.ConnectionAborted);

    public async Task SetReady(bool isReady)
    {
        var result = await lobbyService.SetReadyAsync(PlayerId, isReady, Context.ConnectionAborted);
        var group = Clients.Group(GroupName(result.Lobby.Id));

        if (result.Match is not null)
            await group.MatchStarting(result.Match);
        else
            await group.LobbyUpdated(result.Lobby);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await LeaveCurrentLobbyAsync(CancellationToken.None);
        await base.OnDisconnectedAsync(exception);
    }

    private async Task LeaveCurrentLobbyAsync(CancellationToken ct)
    {
        var result = await lobbyService.LeaveAsync(PlayerId, ct);
        if (result is null)
            return;

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(result.LobbyId));

        if (result.Lobby is not null)
            await Clients.Group(GroupName(result.LobbyId)).LobbyUpdated(result.Lobby);
    }
}