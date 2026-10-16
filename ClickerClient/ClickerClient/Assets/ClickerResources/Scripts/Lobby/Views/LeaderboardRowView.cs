using GameClient.Networking;
using TMPro;
using UnityEngine;

namespace GameClient.Lobby.Views
{
    /// <summary>A leaderboard row (prefab).</summary>
    public class LeaderboardRowView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _rankText;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _scoreText;
        [SerializeField] private GameObject _meHighlight;

        public void Bind(LeaderboardEntry entry, bool isMe)
        {
            _rankText.text = entry.Rank.ToString();
            _nameText.text = entry.Username;
            _scoreText.text = entry.Score.ToString();
            _meHighlight.SetActive(isMe);
        }
    }
}
