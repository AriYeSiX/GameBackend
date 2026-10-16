using GameClient.Networking;
using TMPro;
using UnityEngine;

namespace GameClient.Lobby.Views
{
    /// <summary>A lobby member row (prefab).</summary>
    public class LobbyMemberItemView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private GameObject _readyMark;
        [SerializeField] private GameObject _hostMark;
        [SerializeField] private GameObject _meHighlight;

        public void Bind(LobbyMemberDto member, bool isHost, bool isMe)
        {
            _nameText.text = member.Username;
            _readyMark.SetActive(member.IsReady);
            _hostMark.SetActive(isHost);
            _meHighlight.SetActive(isMe);
        }
    }
}
