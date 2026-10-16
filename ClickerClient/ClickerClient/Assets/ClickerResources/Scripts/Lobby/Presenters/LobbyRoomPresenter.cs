using System.Linq;
using GameClient.Core;
using GameClient.Lobby.Views;
using GameClient.Networking;
using GameClient.UI;
using VContainer.Unity;

namespace GameClient.Lobby.Presenters
{
    /// <summary>
    /// Lobby room. The transition to a match is handled by GameFlow, not this presenter,
    /// on the MatchStarting event, which can arrive at any time.
    /// </summary>
    public sealed class LobbyRoomPresenter : Presenter, IInitializable
    {
        private readonly LobbyRoomView _view;
        private readonly GameHubClient _hub;
        private readonly LobbySession _lobby;
        private readonly UserSession _user;
        private readonly LobbyNavigator _navigator;

        public LobbyRoomPresenter(LobbyRoomView view, GameHubClient hub, LobbySession lobby,
            UserSession user, LobbyNavigator navigator)
        {
            _view = view;
            _hub = hub;
            _lobby = lobby;
            _user = user;
            _navigator = navigator;
        }

        public void Initialize()
        {
            _navigator.Changed += OnScreenChanged;
            _hub.LobbyUpdated += OnLobbyUpdated;
            _view.ReadyClicked += OnReadyClicked;
            _view.LeaveClicked += OnLeaveClicked;
        }

        public override void Dispose()
        {
            _navigator.Changed -= OnScreenChanged;
            _hub.LobbyUpdated -= OnLobbyUpdated;
            _view.ReadyClicked -= OnReadyClicked;
            _view.LeaveClicked -= OnLeaveClicked;
            base.Dispose();
        }

        private bool IsMeReady =>
            _lobby.CurrentLobby?.Members.Any(m => m.PlayerId == _user.PlayerId && m.IsReady) ?? false;

        private void OnScreenChanged(LobbyScreen screen)
        {
            if (screen != LobbyScreen.Room || _lobby.CurrentLobby == null)
                return;

            _view.ShowError(null);
            _view.Render(_lobby.CurrentLobby, _user.PlayerId);
        }

        private void OnLobbyUpdated(LobbyDto lobby)
        {
            if (_lobby.CurrentLobby == null || _lobby.CurrentLobby.Id != lobby.Id)
                return;

            _lobby.CurrentLobby = lobby;

            if (_navigator.Current == LobbyScreen.Room)
                _view.Render(lobby, _user.PlayerId);
        }

        private void OnReadyClicked()
        {
            var ready = !IsMeReady;
            _view.SetBusy(true);

            Run(async () =>
            {
                try
                {
                    await _hub.SetReadyAsync(ready); // the server replies with LobbyUpdated or MatchStarting
                }
                finally
                {
                    if (IsAlive)
                        _view.SetBusy(false);
                }
            }, _view.ShowError);
        }

        private void OnLeaveClicked()
        {
            _view.SetBusy(true);

            Run(async () =>
            {
                try
                {
                    await _hub.LeaveLobbyAsync();
                    Lifetime.ThrowIfCancellationRequested();
                    _lobby.CurrentLobby = null;
                    _navigator.Show(LobbyScreen.Browser);
                }
                finally
                {
                    if (IsAlive)
                        _view.SetBusy(false);
                }
            }, _view.ShowError);
        }
    }
}
