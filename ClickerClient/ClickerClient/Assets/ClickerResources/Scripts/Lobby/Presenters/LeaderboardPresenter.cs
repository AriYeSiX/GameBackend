using GameClient.Core;
using GameClient.Lobby.Views;
using GameClient.Networking;
using GameClient.UI;
using VContainer.Unity;

namespace GameClient.Lobby.Presenters
{
    public sealed class LeaderboardPresenter : Presenter, IInitializable
    {
        private readonly LeaderboardView _view;
        private readonly GameApi _gameApi;
        private readonly UserSession _user;
        private readonly LobbyNavigator _navigator;

        public LeaderboardPresenter(LeaderboardView view, GameApi gameApi, UserSession user, LobbyNavigator navigator)
        {
            _view = view;
            _gameApi = gameApi;
            _user = user;
            _navigator = navigator;
        }

        public void Initialize()
        {
            _navigator.Changed += OnScreenChanged;
            _view.CloseClicked += OnClose;
        }

        public override void Dispose()
        {
            _navigator.Changed -= OnScreenChanged;
            _view.CloseClicked -= OnClose;
            base.Dispose();
        }

        private void OnClose() => _navigator.Show(LobbyScreen.Browser);

        private void OnScreenChanged(LobbyScreen screen)
        {
            if (screen != LobbyScreen.Leaderboard)
                return;

            _view.SetStatus("Loading...");
            _view.SetMyRank(string.Empty);

            Run(async () =>
            {
                var top = await _gameApi.GetTopAsync(GameConstants.LeaderboardKey, GameConstants.LeaderboardSize);
                var mine = await _gameApi.TryGetMyRankAsync(GameConstants.LeaderboardKey);
                Lifetime.ThrowIfCancellationRequested();

                _view.SetEntries(top, _user.PlayerId);
                _view.SetMyRank(mine == null
                    ? "You have not played any matches yet"
                    : $"Your rank: {mine.Rank}, best score: {mine.Score}");
            }, _view.SetStatus);
        }
    }
}
