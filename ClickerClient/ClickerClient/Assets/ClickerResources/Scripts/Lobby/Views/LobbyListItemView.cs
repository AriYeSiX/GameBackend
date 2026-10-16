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

        public void Bind(LobbyDto lobby, Action<Guid> onJoin)
        {
            _nameText.text = lobby.Name;
            _playersText.text = $"{lobby.Members.Count}/{lobby.MaxPlayers}";
            _joinButton.onClick.RemoveAllListeners();
            _joinButton.onClick.AddListener(() => onJoin(lobby.Id));
        }
    }
}
