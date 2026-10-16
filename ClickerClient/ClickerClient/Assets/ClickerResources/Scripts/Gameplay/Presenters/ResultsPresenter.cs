using System.Linq;
using GameClient.Core;
using GameClient.Gameplay.Views;
using GameClient.Networking;
using GameClient.UI;
using VContainer.Unity;

namespace GameClient.Gameplay.Presenters
{
    /// <summary>
    /// Results screen. "Play again" votes for a rematch: the server creates a new match
    /// once all remaining players agree and sends MatchStarting; GameFlow takes over from there.
    /// </summary>
    public sealed class ResultsPresenter : Presenter, IStartable
    {
        private readonly ResultsView _view;
        private readonly MatchSession _match;
        private readonly GameHubClient _hub;
        private readonly UserSession _user;
        private readonly GameFlow _flow;

        public ResultsPresenter(ResultsView view, MatchSession match, GameHubClient hub, UserSession user, GameFlow flow)
        {
            _view = view;
            _match = match;
            _hub = hub;
            _user = user;
            _flow = flow;
        }

        public void Start()
        {
            _match.Finished += OnFinished;
            _match.RematchChanged += OnRematchChanged;
            _match.Closed += OnClosed;
            _view.PlayAgainClicked += OnPlayAgain;
            _view.BackToLobbyClicked += OnBackToLobby;

            _view.SetVisible(false);

            if (_match.Results != null)
                OnFinished();
        }

        public override void Dispose()
        {
            _match.Finished -= OnFinished;
            _match.RematchChanged -= OnRematchChanged;
            _match.Closed -= OnClosed;
            _view.PlayAgainClicked -= OnPlayAgain;
            _view.BackToLobbyClicked -= OnBackToLobby;
            base.Dispose();
        }

        private void OnFinished()
        {
            var results = _match.Results.Results;
            var me = results.FirstOrDefault(r => r.PlayerId == _user.PlayerId);
            var winners = results.Count(r => r.IsWinner);

            var title = me == null ? "Match over"
                : !me.IsWinner ? (winners == 0 ? "Draw" : "Defeat")
                : winners > 1 ? "Draw" : "Victory!";

            _view.ShowResults(title, results, _user.PlayerId);
            _view.SetRematchStatus(null);
            _view.SetPlayAgainInteractable(true);
            _view.SetBackInteractable(true);
            _view.SetVisible(true);
        }

        private void OnRematchChanged()
        {
            var status = _match.Rematch;
            var iAmReady = status.ReadyPlayerIds.Contains(_user.PlayerId);

            _view.SetRematchStatus(iAmReady
                ? $"Waiting for others: {status.ReadyPlayerIds.Count}/{status.ActivePlayerIds.Count}"
                : $"Want to play again: {status.ReadyPlayerIds.Count}/{status.ActivePlayerIds.Count}");
        }

        private void OnClosed(string reason)
        {
            _view.SetPlayAgainInteractable(false);
            _view.SetRematchStatus($"{reason}. Rematch is not available");
        }

        private void OnPlayAgain()
        {
            _view.SetPlayAgainInteractable(false);
            _view.SetRematchStatus("Waiting for other players...");

            var matchId = _match.MatchId;
            Run(() => _hub.RequestRematchAsync(matchId), error =>
            {
                _view.SetRematchStatus(error);
                _view.SetPlayAgainInteractable(_match.ClosedReason == null);
            });
        }

        private void OnBackToLobby()
        {
            _view.SetPlayAgainInteractable(false);
            _view.SetBackInteractable(false);
            Run(_flow.ReturnToLobbyAsync);
        }
    }
}
