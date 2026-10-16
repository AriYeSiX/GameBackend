using System.Collections.Generic;
using System.Linq;
using GameClient.Networking;
using TMPro;
using UnityEngine;

namespace GameClient.Loading
{
    public class LoadingView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _statusText;
        [SerializeField] private TMP_Text _playersText;

        public void SetStatus(string text) => _statusText.text = text;

        public void SetPlayers(IEnumerable<MatchPlayerDto> players) =>
            _playersText.text = string.Join("\n", players.Select(p => p.Username));
    }
}
