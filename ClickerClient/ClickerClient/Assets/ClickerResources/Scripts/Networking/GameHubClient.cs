using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using UnityEngine;

namespace GameClient.Networking
{
    /// <summary>
    /// Connection to GameHub. All events are raised on the Unity main thread.
    /// </summary>
    public sealed class GameHubClient : IDisposable
    {
        private readonly HubConnection _connection;

        public event Action<LobbyDto> LobbyUpdated;
        public event Action<MatchStartingDto> MatchStarting;
        public event Action<MatchStartedDto> MatchStarted;
        public event Action<ScoreChangedDto> ScoreChanged;
        public event Action<MatchFinishedDto> MatchFinished;
        public event Action<RematchStatusDto> RematchStatus;
        public event Action<MatchClosedDto> MatchClosed;
        public event Action ConnectionLost;

        public GameHubClient(ApiClient api)
        {
            _connection = new HubConnectionBuilder()
                .WithUrl(new Uri(api.BaseAddress, "hubs/game"), options =>
                    options.AccessTokenProvider = api.GetFreshAccessTokenAsync)
                .Build();

            Subscribe<LobbyDto>("LobbyUpdated", x => LobbyUpdated?.Invoke(x));
            Subscribe<MatchStartingDto>("MatchStarting", x => MatchStarting?.Invoke(x));
            Subscribe<MatchStartedDto>("MatchStarted", x => MatchStarted?.Invoke(x));
            Subscribe<ScoreChangedDto>("ScoreChanged", x => ScoreChanged?.Invoke(x));
            Subscribe<MatchFinishedDto>("MatchFinished", x => MatchFinished?.Invoke(x));
            Subscribe<RematchStatusDto>("RematchStatus", x => RematchStatus?.Invoke(x));
            Subscribe<MatchClosedDto>("MatchClosed", x => MatchClosed?.Invoke(x));

            _connection.Closed += async _ =>
            {
                await Awaitable.MainThreadAsync();
                ConnectionLost?.Invoke();
            };
        }

        public bool IsConnected => _connection.State == HubConnectionState.Connected;

        public async Task ConnectAsync()
        {
            if (_connection.State == HubConnectionState.Disconnected)
                await _connection.StartAsync();
        }

        // ---------- Lobby ----------

        public Task<LobbyDto> CreateLobbyAsync(string name, int maxPlayers) =>
            _connection.InvokeAsync<LobbyDto>("CreateLobby", name, maxPlayers);

        public Task<LobbyDto> JoinLobbyAsync(Guid lobbyId) =>
            _connection.InvokeAsync<LobbyDto>("JoinLobby", lobbyId);

        public Task LeaveLobbyAsync() =>
            _connection.InvokeAsync("LeaveLobby");

        public Task SetReadyAsync(bool isReady) =>
            _connection.InvokeAsync("SetReady", isReady);

        // ---------- Match ----------

        public Task MatchLoadedAsync(Guid matchId) =>
            _connection.InvokeAsync("MatchLoaded", matchId);

        /// <summary>Send instead of Invoke: we do not wait for a server response on every click.</summary>
        public Task SendClickAsync(Guid matchId) =>
            _connection.SendAsync("Click", matchId);

        public Task RequestRematchAsync(Guid matchId) =>
            _connection.InvokeAsync("RequestRematch", matchId);

        public Task LeaveMatchAsync() =>
            _connection.InvokeAsync("LeaveMatch");

        public void Dispose() => _ = _connection.DisposeAsync();

        // SignalR calls handlers off the main thread, so switch to it before touching Unity
        private void Subscribe<T>(string method, Action<T> raise) =>
            _connection.On<T>(method, async payload =>
            {
                await Awaitable.MainThreadAsync();
                raise(payload);
            });
    }
}
