using System;
using System.Threading.Tasks;
using GameClient.Networking;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer.Unity;

namespace GameClient.Core
{
    /// <summary>
    /// Game flow on top of hub events: scene transitions and filling <see cref="MatchSession"/>. 
    /// Lives in the root scope, so no events are lost during scene changes.
    /// </summary>
    public sealed class GameFlow : IStartable, IDisposable
    {
        private static readonly TimeSpan StartWaitTimeout = TimeSpan.FromSeconds(30);

        private readonly GameHubClient _hub;
        private readonly MatchSession _match;
        private readonly LobbySession _lobby;

        // true — the match has started; false — the match was closed while we were waiting during the loading phase
        private TaskCompletionSource<bool> _startWait;
        private bool _disposed;

        public GameFlow(GameHubClient hub, MatchSession match, LobbySession lobby)
        {
            _hub = hub;
            _match = match;
            _lobby = lobby;
        }

        public void Start()
        {
            _hub.MatchStarting += OnMatchStarting;
            _hub.MatchStarted += OnMatchStarted;
            _hub.ScoreChanged += _match.Apply;
            _hub.MatchFinished += _match.Apply;
            _hub.RematchStatus += _match.Apply;
            _hub.MatchClosed += OnMatchClosed;
            _hub.ConnectionLost += OnConnectionLost;
        }

        public void Dispose()
        {
            _disposed = true;
            _hub.MatchStarting -= OnMatchStarting;
            _hub.MatchStarted -= OnMatchStarted;
            _hub.ScoreChanged -= _match.Apply;
            _hub.MatchFinished -= _match.Apply;
            _hub.RematchStatus -= _match.Apply;
            _hub.MatchClosed -= OnMatchClosed;
            _hub.ConnectionLost -= OnConnectionLost;
        }

        /// <summary>"To Lobby" button on the results screen.</summary>
        public async Task ReturnToLobbyAsync()
        {
            try
            {
                await _hub.LeaveMatchAsync();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"LeaveMatch failed: {ex.Message}"); // the server will remove us on disconnect anyway
            }

            GoToLobby(null);
        }

        private void OnMatchStarting(MatchStartingDto match)
        {
            _lobby.CurrentLobby = null;
            _match.Begin(match);
            RunLoading(match.MatchId);
        }

        private void OnMatchStarted(MatchStartedDto started)
        {
            if (_match.Apply(started, Time.realtimeSinceStartupAsDouble))
                _startWait?.TrySetResult(true);
        }

        private void OnMatchClosed(MatchClosedDto closed)
        {
            if (closed.MatchId != _match.MatchId)
                return;

            if (_match.Results != null)
            {
                // We are on the results screen: we remain there, but a rematch is no longer possible.
                _match.MarkClosed(closed.Reason);
                return;
            }

            _startWait?.TrySetResult(false);
            GoToLobby(closed.Reason);
        }

        private void OnConnectionLost()
        {
            // When exiting Play Mode, the connection also closes — this is not an error.
            if (_disposed || !Application.isPlaying)
                return;

            GoToLobby("Connection to the server has been lost");
        }

        private async void RunLoading(Guid matchId)
        {
            var startWait = _startWait = new TaskCompletionSource<bool>();

            try
            {
                await SceneManager.LoadSceneAsync(SceneNames.Loading, LoadSceneMode.Single);

                // We load the gameplay additively beneath the loading screen: the scene is ready, but remains hidden for now.
                await SceneManager.LoadSceneAsync(SceneNames.Gameplay, LoadSceneMode.Additive);

                if (_match.MatchId != matchId)
                    return;

                await _hub.MatchLoadedAsync(matchId);

                var finished = await Task.WhenAny(startWait.Task, Task.Delay(StartWaitTimeout));
                if (finished != startWait.Task)
                {
                    await LeaveAndGoToLobbyAsync("The server did not start the match on time");
                    return;
                }

                if (!startWait.Task.Result || _match.MatchId != matchId)
                    return; // The match was closed; OnMatchClosed has already sent us to the lobby.

                SceneManager.SetActiveScene(SceneManager.GetSceneByName(SceneNames.Gameplay));
                await SceneManager.UnloadSceneAsync(SceneNames.Loading);
            }
            catch (Exception ex)
            {
                if (_match.MatchId != matchId)
                    return; // The match is already closed and we were sent to the lobby with a clear reason.

                Debug.LogException(ex);
                await LeaveAndGoToLobbyAsync("Failed to start the match");
            }
        }

        private async Task LeaveAndGoToLobbyAsync(string notice)
        {
            try
            {
                await _hub.LeaveMatchAsync();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"LeaveMatch failed: {ex.Message}");
            }

            GoToLobby(notice);
        }

        private void GoToLobby(string notice)
        {
            if (_disposed)
                return;

            _match.Reset();
            _lobby.CurrentLobby = null;
            _lobby.PendingNotice = notice;
            SceneManager.LoadSceneAsync(SceneNames.Lobby, LoadSceneMode.Single);
        }
    }
}
