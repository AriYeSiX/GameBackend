using System;
using System.Collections.Generic;
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
        private readonly List<IDisposable> _handlers = new();

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

            Subscribe<LobbyDto>("LobbyUpdated", RaiseLobbyUpdated);
            Subscribe<MatchStartingDto>("MatchStarting", RaiseMatchStarting);
            Subscribe<MatchStartedDto>("MatchStarted", RaiseMatchStarted);
            Subscribe<ScoreChangedDto>("ScoreChanged", RaiseScoreChanged);
            Subscribe<MatchFinishedDto>("MatchFinished", RaiseMatchFinished);
            Subscribe<RematchStatusDto>("RematchStatus", RaiseRematchStatus);
            Subscribe<MatchClosedDto>("MatchClosed", RaiseMatchClosed);

            _connection.Closed += OnConnectionClosed;
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

        public void Dispose()
        {
            // Unsubscribe first: disposing the connection raises Closed, and nobody should react to it on shutdown
            _connection.Closed -= OnConnectionClosed;

            foreach (var handler in _handlers)
                handler.Dispose();

            _handlers.Clear();
            _ = _connection.DisposeAsync();
        }

        // SignalR calls handlers off the main thread, so switch to it before touching Unity.
        // On() returns a subscription handle; we keep it to remove the handler in Dispose.
        private void Subscribe<T>(string method, Action<T> raise) =>
            _handlers.Add(_connection.On<T>(method, async payload =>
            {
                await Awaitable.MainThreadAsync();
                raise(payload);
            }));

        private async Task OnConnectionClosed(Exception error)
        {
            await Awaitable.MainThreadAsync();
            ConnectionLost?.Invoke();
        }

        private void RaiseLobbyUpdated(LobbyDto payload) => LobbyUpdated?.Invoke(payload);

        private void RaiseMatchStarting(MatchStartingDto payload) => MatchStarting?.Invoke(payload);

        private void RaiseMatchStarted(MatchStartedDto payload) => MatchStarted?.Invoke(payload);

        private void RaiseScoreChanged(ScoreChangedDto payload) => ScoreChanged?.Invoke(payload);

        private void RaiseMatchFinished(MatchFinishedDto payload) => MatchFinished?.Invoke(payload);

        private void RaiseRematchStatus(RematchStatusDto payload) => RematchStatus?.Invoke(payload);

        private void RaiseMatchClosed(MatchClosedDto payload) => MatchClosed?.Invoke(payload);
    }
}
