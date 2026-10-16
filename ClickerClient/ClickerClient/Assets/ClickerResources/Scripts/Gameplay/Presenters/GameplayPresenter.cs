using System;
using GameClient.Core;
using GameClient.Gameplay.Views;
using GameClient.Networking;
using GameClient.UI;
using UnityEngine;
using VContainer.Unity;

namespace GameClient.Gameplay.Presenters
{
    /// <summary>
    /// Countdown, round and live score. Time is measured from the moment MatchStarted is received
    /// using the local clock, so the start differs between players only by network latency.
    /// The score on screen is the one sent by the server: the client only reports clicks.
    /// </summary>
    public sealed class GameplayPresenter : Presenter, IStartable, ITickable
    {
        private const double GoLabelSeconds = 0.6;

        private enum Phase { Waiting, Countdown, Round, TimeUp }

        private readonly GameplayView _view;
        private readonly MatchSession _match;
        private readonly GameHubClient _hub;
        private readonly UserSession _user;

        private Phase _phase = Phase.Waiting;

        public GameplayPresenter(GameplayView view, MatchSession match, GameHubClient hub, UserSession user)
        {
            _view = view;
            _match = match;
            _hub = hub;
            _user = user;
        }

        public void Start()
        {
            _match.Started += OnStarted;
            _match.ScoreChanged += OnScoreChanged;
            _view.Button.Clicked += OnButtonClicked;

            _view.SetCountdown(null);
            _view.SetMessage(null);
            ShowPlayers();

            // The scene loads under the loading screen, so show it only once the match has started
            _view.SetVisible(_match.IsStarted);
        }

        public override void Dispose()
        {
            _match.Started -= OnStarted;
            _match.ScoreChanged -= OnScoreChanged;
            _view.Button.Clicked -= OnButtonClicked;
            base.Dispose();
        }

        public void Tick()
        {
            if (!_match.IsStarted || _phase == Phase.TimeUp)
                return;

            var now = Time.realtimeSinceStartupAsDouble;

            if (now < _match.CountdownEndsAt)
            {
                _phase = Phase.Countdown;
                _view.SetCountdown(Math.Ceiling(_match.CountdownEndsAt - now).ToString("0"));
                _view.SetTimeLeft(_match.StartInfo.DurationMs / 1000f);
                return;
            }

            if (now < _match.RoundEndsAt)
            {
                if (_phase != Phase.Round)
                {
                    _phase = Phase.Round;
                    _view.Button.Begin(_match.StartInfo.DurationMs / 1000f);
                }

                _view.SetCountdown(now - _match.CountdownEndsAt < GoLabelSeconds ? "Go!" : null);
                _view.SetTimeLeft((float)(_match.RoundEndsAt - now));
                return;
            }

            _phase = Phase.TimeUp;
            _view.Button.Stop();
            _view.SetCountdown(null);
            _view.SetTimeLeft(0f);
            _view.SetMessage("Time's up! Counting the results...");
        }

        private void OnStarted()
        {
            ShowPlayers(); // someone may have dropped out during loading
            _view.SetVisible(true);
        }

        private void ShowPlayers()
        {
            _view.SetPlayers(_match.Players, _user.PlayerId);

            foreach (var player in _match.Players)
                _view.SetScore(player.PlayerId, _match.GetScore(player.PlayerId));
        }

        private void OnScoreChanged(Guid playerId, long score) => _view.SetScore(playerId, score);

        private void OnButtonClicked()
        {
            if (_phase != Phase.Round)
                return;

            var matchId = _match.MatchId;
            Run(() => _hub.SendClickAsync(matchId));
        }
    }
}
