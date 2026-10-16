using System;
using System.Collections.Generic;
using GameClient.Networking;
using GameClient.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameClient.Lobby.Views
{
    public class LeaderboardView : ScreenView
    {
        [SerializeField] private Transform _rowsRoot;
        [SerializeField] private LeaderboardRowView _rowPrefab;
        [SerializeField] private TMP_Text _myRankText;
        [SerializeField] private TMP_Text _statusText; // "Loading...", errors, "Nobody has played yet"
        [SerializeField] private Button _closeButton;

        public event Action CloseClicked;

        private void Awake()
        {
            _closeButton.onClick.AddListener(OnCloseClicked);
        }

        private void OnDestroy()
        {
            _closeButton.onClick.RemoveListener(OnCloseClicked);
        }

        private void OnCloseClicked() => CloseClicked?.Invoke();

        public void SetEntries(IReadOnlyList<LeaderboardEntry> entries, Guid me)
        {
            UiList.Rebuild(_rowsRoot, _rowPrefab, entries, (row, entry) => row.Bind(entry, entry.PlayerId == me));
            _statusText.text = entries.Count == 0 ? "Nobody has played yet" : string.Empty;
        }

        public void SetMyRank(string text) => _myRankText.text = text;

        public void SetStatus(string text) => _statusText.text = text ?? string.Empty;
    }
}
