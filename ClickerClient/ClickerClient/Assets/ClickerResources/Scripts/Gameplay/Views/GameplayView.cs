using System;
using System.Collections.Generic;
using GameClient.Networking;
using GameClient.UI;
using TMPro;
using UnityEngine;

namespace GameClient.Gameplay.Views
{
    public class GameplayView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _root;        // hides everything until the match starts
        [SerializeField] private TMP_Text _countdownText;  // 3, 2, 1, "Go!"
        [SerializeField] private TMP_Text _timerText;      // time left in the round
        [SerializeField] private TMP_Text _messageText;    // "Time's up..."
        [SerializeField] private Transform _scoresRoot;
        [SerializeField] private ScoreRowView _scoreRowPrefab;
        [SerializeField] private MovingButton _button;

        private readonly Dictionary<Guid, ScoreRowView> _rows = new();

        public MovingButton Button => _button;

        public void SetVisible(bool visible)
        {
            _root.alpha = visible ? 1f : 0f;
            _root.interactable = visible;
            _root.blocksRaycasts = visible;
        }

        // Called every frame: update the text only when the shown value changes,
        // and use TMP SetText with arguments to avoid string allocations
        private string _countdownLabel = string.Empty;
        private int _countdownNumber = -1;
        private int _timeLeftTenths = -1;

        public void SetCountdown(string text)
        {
            if (_countdownNumber < 0 && text == _countdownLabel)
                return;

            _countdownNumber = -1;
            _countdownLabel = text;
            _countdownText.gameObject.SetActive(!string.IsNullOrEmpty(text));
            _countdownText.text = text;
        }

        public void SetCountdown(int seconds)
        {
            if (seconds == _countdownNumber)
                return;

            _countdownNumber = seconds;
            _countdownLabel = null;
            _countdownText.gameObject.SetActive(true);
            _countdownText.SetText("{0:0}", seconds);
        }

        public void SetTimeLeft(float seconds)
        {
            var tenths = Mathf.CeilToInt(seconds * 10f);
            if (tenths == _timeLeftTenths)
                return;

            _timeLeftTenths = tenths;
            _timerText.SetText("{0:1}", tenths / 10f);
        }

        public void SetMessage(string text) => _messageText.text = text ?? string.Empty;

        public void SetPlayers(IReadOnlyList<MatchPlayerDto> players, Guid me)
        {
            _rows.Clear();
            var rows = UiList.Rebuild(_scoresRoot, _scoreRowPrefab, players,
                (row, player) => row.Bind(player.Username, 0, player.PlayerId == me));

            for (var i = 0; i < players.Count; i++)
                _rows[players[i].PlayerId] = rows[i];
        }

        public void SetScore(Guid playerId, long score)
        {
            if (_rows.TryGetValue(playerId, out var row))
                row.SetScore(score);
        }
    }
}
