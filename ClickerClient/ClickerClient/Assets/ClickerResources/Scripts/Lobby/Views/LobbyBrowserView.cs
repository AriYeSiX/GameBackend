using System;
using System.Collections.Generic;
using GameClient.Networking;
using GameClient.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameClient.Lobby.Views
{
    /// <summary>Open lobby list, lobby creation and the leaderboard button.</summary>
    public class LobbyBrowserView : ScreenView
    {
        [SerializeField] private TMP_Text _playerNameText;
        [SerializeField] private TMP_Text _messageText;     // notices and errors

        [Header("Lobby creation")]
        [SerializeField] private TMP_InputField _lobbyNameInput;
        [SerializeField] private TMP_InputField _maxPlayersInput;  // Content Type: Integer Number
        [SerializeField] private Button _createButton;

        [Header("Lobby list")]
        [SerializeField] private Transform _listRoot;
        [SerializeField] private LobbyListItemView _itemPrefab;
        [SerializeField] private GameObject _emptyListLabel;
        [SerializeField] private Button _refreshButton;

        [SerializeField] private Button _leaderboardButton;

        public event Action<string, string> CreateRequested; // name, max players (as a string, validated by the presenter)
        public event Action<Guid> JoinRequested;
        public event Action RefreshRequested;
        public event Action LeaderboardRequested;

        private void Awake()
        {
            _createButton.onClick.AddListener(() =>
                CreateRequested?.Invoke(_lobbyNameInput.text.Trim(), _maxPlayersInput.text.Trim()));
            _refreshButton.onClick.AddListener(() => RefreshRequested?.Invoke());
            _leaderboardButton.onClick.AddListener(() => LeaderboardRequested?.Invoke());
        }

        private void OnDestroy()
        {
            _createButton.onClick.RemoveAllListeners();
            _refreshButton.onClick.RemoveAllListeners();
            _leaderboardButton.onClick.RemoveAllListeners();
        }

        public void SetPlayerName(string username) => _playerNameText.text = username;

        public void ShowMessage(string message) => _messageText.text = message ?? string.Empty;

        public void SetBusy(bool busy) => _createButton.interactable = !busy;

        public void SetLobbies(IReadOnlyList<LobbyDto> lobbies)
        {
            UiList.Rebuild(_listRoot, _itemPrefab, lobbies,
                (item, lobby) => item.Bind(lobby, id => JoinRequested?.Invoke(id)));

            _emptyListLabel.SetActive(lobbies.Count == 0);
        }
    }
}
