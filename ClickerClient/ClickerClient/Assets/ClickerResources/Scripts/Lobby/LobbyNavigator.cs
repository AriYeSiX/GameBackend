using System;
using GameClient.Lobby.Views;

namespace GameClient.Lobby
{
    public enum LobbyScreen
    {
        Auth,
        Browser,
        Room,
        Leaderboard
    }

    /// <summary>Switches the lobby scene panels: only one is visible at a time.</summary>
    public sealed class LobbyNavigator
    {
        private readonly AuthView _auth;
        private readonly LobbyBrowserView _browser;
        private readonly LobbyRoomView _room;
        private readonly LeaderboardView _leaderboard;

        public LobbyNavigator(AuthView auth, LobbyBrowserView browser, LobbyRoomView room, LeaderboardView leaderboard)
        {
            _auth = auth;
            _browser = browser;
            _room = room;
            _leaderboard = leaderboard;
        }

        public event Action<LobbyScreen> Changed;

        public LobbyScreen? Current { get; private set; }

        public void Show(LobbyScreen screen)
        {
            Current = screen;

            _auth.SetVisible(screen == LobbyScreen.Auth);
            _browser.SetVisible(screen == LobbyScreen.Browser);
            _room.SetVisible(screen == LobbyScreen.Room);
            _leaderboard.SetVisible(screen == LobbyScreen.Leaderboard);

            Changed?.Invoke(screen);
        }
    }
}
