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

        public void SetCountdown(string text)
        {
            _countdownText.gameObject.SetActive(!string.IsNullOrEmpty(text));
            _countdownText.text = text;
        }

        public void SetTimeLeft(float seconds) => _timerText.text = seconds.ToString("0.0");

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
