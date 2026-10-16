using GameClient.Core;
using GameClient.Lobby.Views;
using GameClient.Networking;
using GameClient.UI;
using VContainer.Unity;

namespace GameClient.Lobby.Presenters
{
    /// <summary>
    /// Decides which panel to show when the scene opens:
    /// on first launch, sign-in; when returning from a match, the lobby list.
    /// </summary>
    public sealed class LobbyEntryPresenter : Presenter, IStartable
    {
        private readonly LobbyNavigator _navigator;
        private readonly UserSession _user;
        private readonly GameHubClient _hub;
        private readonly AuthView _authView;

        public LobbyEntryPresenter(LobbyNavigator navigator, UserSession user, GameHubClient hub, AuthView authView)
        {
            _navigator = navigator;
            _user = user;
            _hub = hub;
            _authView = authView;
        }

        public void Start()
        {
            if (!_user.IsSignedIn)
            {
                _navigator.Show(LobbyScreen.Auth);
                return;
            }

            Run(async () =>
            {
                await _hub.ConnectAsync(); // reconnect after the connection was lost
                Lifetime.ThrowIfCancellationRequested();
                _navigator.Show(LobbyScreen.Browser);
            },
            error =>
            {
                _navigator.Show(LobbyScreen.Auth);
                _authView.ShowError($"Failed to connect: {error}");
            });
        }
    }
}
