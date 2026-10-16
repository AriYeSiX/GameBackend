using System;
using System.Threading.Tasks;
using GameClient.Core;
using GameClient.Lobby.Views;
using GameClient.Networking;
using GameClient.UI;
using UnityEngine;
using VContainer.Unity;

namespace GameClient.Lobby.Presenters
{
    /// <summary>List of open lobbies: auto refresh, creating and joining.</summary>
    public sealed class LobbyBrowserPresenter : Presenter, IInitializable
    {
        private const float AutoRefreshSeconds = 3f;

        private readonly LobbyBrowserView _view;
        private readonly GameApi _gameApi;
        private readonly GameHubClient _hub;
        private readonly UserSession _user;
        private readonly LobbySession _lobby;
        private readonly LobbyNavigator _navigator;

        private int _visitId; // changes every time the panel is shown, so the old auto refresh loop stops by itself

        public LobbyBrowserPresenter(LobbyBrowserView view, GameApi gameApi, GameHubClient hub,
            UserSession user, LobbySession lobby, LobbyNavigator navigator)
        {
            _view = view;
            _gameApi = gameApi;
            _hub = hub;
            _user = user;
            _lobby = lobby;
            _navigator = navigator;
        }

        public void Initialize()
        {
            _navigator.Changed += OnScreenChanged;
            _view.CreateRequested += OnCreate;
            _view.JoinRequested += OnJoin;
            _view.RefreshRequested += OnRefresh;
            _view.LeaderboardRequested += OnLeaderboard;
        }

        public override void Dispose()
        {
            _navigator.Changed -= OnScreenChanged;
            _view.CreateRequested -= OnCreate;
            _view.JoinRequested -= OnJoin;
            _view.RefreshRequested -= OnRefresh;
            _view.LeaderboardRequested -= OnLeaderboard;
            base.Dispose();
        }

        private void OnScreenChanged(LobbyScreen screen)
        {
            _visitId++;

            if (screen != LobbyScreen.Browser)
                return;

            _view.SetPlayerName(_user.Username);
            _view.ShowMessage(_lobby.TakeNotice());
            AutoRefresh(_visitId);
        }

        private void AutoRefresh(int visitId)
        {
            Run(async () =>
            {
                while (visitId == _visitId)
                {
                    await RefreshAsync();
                    await Awaitable.WaitForSecondsAsync(AutoRefreshSeconds, Lifetime);
                }
            }, _view.ShowMessage); // on error the loop stops; the list can be refreshed with the button
        }

        private void OnRefresh() => Run(RefreshAsync, _view.ShowMessage);

        private async Task RefreshAsync()
        {
            var lobbies = await _gameApi.GetOpenLobbiesAsync();
            Lifetime.ThrowIfCancellationRequested();
            _view.SetLobbies(lobbies);
        }

        private void OnCreate(string name, string maxPlayersText)
        {
            if (name.Length is < 3 or > 40)
            {
                _view.ShowMessage("Lobby name must be 3 to 40 characters long");
                return;
            }

            if (!int.TryParse(maxPlayersText, out var maxPlayers) || maxPlayers is < 2 or > 16)
            {
                _view.ShowMessage("Players per lobby: 2 to 16");
                return;
            }

            EnterLobby(() => _hub.CreateLobbyAsync(name, maxPlayers));
        }

        private void OnJoin(Guid lobbyId) => EnterLobby(() => _hub.JoinLobbyAsync(lobbyId));

        private void OnLeaderboard() => _navigator.Show(LobbyScreen.Leaderboard);

        private void EnterLobby(Func<Task<LobbyDto>> enter)
        {
            _view.ShowMessage(null);
            _view.SetBusy(true);

            Run(async () =>
            {
                try
                {
                    var lobby = await enter();
                    Lifetime.ThrowIfCancellationRequested();
                    _lobby.CurrentLobby = lobby;
                    _navigator.Show(LobbyScreen.Room);
                }
                finally
                {
                    if (IsAlive)
                        _view.SetBusy(false);
                }
            }, _view.ShowMessage);
        }
    }
}
