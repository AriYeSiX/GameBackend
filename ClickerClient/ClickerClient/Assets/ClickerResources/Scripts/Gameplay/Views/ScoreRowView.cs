using TMPro;
using UnityEngine;

namespace GameClient.Gameplay.Views
{
    /// <summary>A live score row during the round (prefab).</summary>
    public class ScoreRowView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _scoreText;
        [SerializeField] private GameObject _meHighlight;

        public void Bind(string username, long score, bool isMe)
        {
            _nameText.text = username;
            _meHighlight.SetActive(isMe);
            SetScore(score);
        }

        public void SetScore(long score) => _scoreText.text = score.ToString();
    }
}
