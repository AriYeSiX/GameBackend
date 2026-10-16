using GameClient.Networking;
using TMPro;
using UnityEngine;

namespace GameClient.Gameplay.Views
{
    /// <summary>A row of the match results table (prefab).</summary>
    public class ResultRowView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _placeText;
        [SerializeField] private TMP_Text _nameText;
        [SerializeField] private TMP_Text _scoreText;
        [SerializeField] private TMP_Text _leaderboardText;  // "New record! Leaderboard rank: 12"
        [SerializeField] private GameObject _winnerMark;     // crown, cup, etc.
        [SerializeField] private GameObject _meHighlight;

        public void Bind(int place, PlayerResultDto result, bool isMe)
        {
            _placeText.text = place.ToString();
            _nameText.text = result.Username;
            _scoreText.text = result.Score.ToString();
            _winnerMark.SetActive(result.IsWinner);
            _meHighlight.SetActive(isMe);

            _leaderboardText.text = result.IsNewBest
                ? $"New record! Leaderboard rank: {result.Rank}"
                : $"Best: {result.BestScore}, rank: {result.Rank}";
        }
    }
}
