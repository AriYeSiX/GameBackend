using System;
using GameClient.Networking;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameClient.Lobby.Views
{
    /// <summary>A lobby list row (prefab).</summary>
    public class LobbyListItemView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _playersText;
        [SerializeField] private Button _joinButton;

        private Guid _lobbyId;
        private Action<Guid> _onJoin;

        private void Awake() => _joinButton.onClick.AddListener(OnJoinClicked);

        private void OnDestroy() => _joinButton.onClick.RemoveListener(OnJoinClicked);

        public void Bind(LobbyDto lobby, Action<Guid> onJoin)
        {
            _lobbyId = lobby.Id;
            _onJoin = onJoin;
            _nameText.text = lobby.Name;
            _playersText.text = $"{lobby.Members.Count}/{lobby.MaxPlayers}";
        }

        private void OnJoinClicked() => _onJoin?.Invoke(_lobbyId);
    }
}
