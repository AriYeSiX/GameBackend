using System;
using System.Linq;
using GameClient.Networking;
using GameClient.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameClient.Lobby.Views
{
    /// <summary>Lobby room: members, the Ready button and Leave.</summary>
    public class LobbyRoomView : ScreenView
    {
        [SerializeField] private TMP_Text _lobbyNameText;
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private Transform _membersRoot;
        [SerializeField] private LobbyMemberItemView _memberPrefab;
        [SerializeField] private Button _readyButton;
        [SerializeField] private TMP_Text _readyButtonText;
        [SerializeField] private Button _leaveButton;
        [SerializeField] private TMP_Text _errorText;

        public event Action ReadyClicked;
        public event Action LeaveClicked;

        private void Awake()
        {
            _readyButton.onClick.AddListener(OnReadyClicked);
            _leaveButton.onClick.AddListener(OnLeaveClicked);
        }

        private void OnDestroy()
        {
            _readyButton.onClick.RemoveListener(OnReadyClicked);
            _leaveButton.onClick.RemoveListener(OnLeaveClicked);
        }

        private void OnReadyClicked() => ReadyClicked?.Invoke();

        private void OnLeaveClicked() => LeaveClicked?.Invoke();

        public void Render(LobbyDto lobby, Guid me)
        {
            _lobbyNameText.text = lobby.Name;

            UiList.Rebuild(_membersRoot, _memberPrefab, lobby.Members,
                (item, member) => item.Bind(member, member.PlayerId == lobby.HostId, member.PlayerId == me));

            var readyCount = lobby.Members.Count(m => m.IsReady);
            _statusText.text = lobby.Members.Count < 2
                ? $"Waiting for players: {lobby.Members.Count}/{lobby.MaxPlayers}"
                : $"Ready: {readyCount}/{lobby.Members.Count}";

            var meReady = lobby.Members.Any(m => m.PlayerId == me && m.IsReady);
            _readyButtonText.text = meReady ? "Not ready" : "Ready";
        }

        public void SetBusy(bool busy)
        {
            _readyButton.interactable = !busy;
            _leaveButton.interactable = !busy;
        }

        public void ShowError(string message) => _errorText.text = message ?? string.Empty;
    }
}
