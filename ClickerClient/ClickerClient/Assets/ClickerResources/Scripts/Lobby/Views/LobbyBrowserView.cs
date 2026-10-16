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
            _createButton.onClick.AddListener(OnCreateClicked);
            _refreshButton.onClick.AddListener(OnRefreshClicked);
            _leaderboardButton.onClick.AddListener(OnLeaderboardClicked);
        }

        private void OnDestroy()
        {
            _createButton.onClick.RemoveListener(OnCreateClicked);
            _refreshButton.onClick.RemoveListener(OnRefreshClicked);
            _leaderboardButton.onClick.RemoveListener(OnLeaderboardClicked);
        }

        public void SetPlayerName(string username) => _playerNameText.text = username;

        public void ShowMessage(string message) => _messageText.text = message ?? string.Empty;

        public void SetBusy(bool busy) => _createButton.interactable = !busy;

        public void SetLobbies(IReadOnlyList<LobbyDto> lobbies)
        {
            UiList.Rebuild(_listRoot, _itemPrefab, lobbies, BindItem);
            _emptyListLabel.SetActive(lobbies.Count == 0);
        }

        private void BindItem(LobbyListItemView item, LobbyDto lobby) => item.Bind(lobby, OnJoinClicked);

        private void OnCreateClicked() =>
            CreateRequested?.Invoke(_lobbyNameInput.text.Trim(), _maxPlayersInput.text.Trim());

        private void OnRefreshClicked() => RefreshRequested?.Invoke();

        private void OnLeaderboardClicked() => LeaderboardRequested?.Invoke();

        private void OnJoinClicked(Guid lobbyId) => JoinRequested?.Invoke(lobbyId);
    }
}
