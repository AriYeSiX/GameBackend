using System;
using System.Collections.Generic;
using GameClient.Networking;
using GameClient.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameClient.Gameplay.Views
{
    /// <summary>Results screen on top of the gameplay.</summary>
    public class ResultsView : ScreenView
    {
        [SerializeField] private TMP_Text _titleText;          // "Victory!", "Defeat", "Draw"
        [SerializeField] private Transform _rowsRoot;
        [SerializeField] private ResultRowView _rowPrefab;
        [SerializeField] private TMP_Text _rematchStatusText;
        [SerializeField] private Button _playAgainButton;
        [SerializeField] private Button _backToLobbyButton;

        public event Action PlayAgainClicked;
        public event Action BackToLobbyClicked;

        private void Awake()
        {
            _playAgainButton.onClick.AddListener(OnPlayAgainClicked);
            _backToLobbyButton.onClick.AddListener(OnBackToLobbyClicked);
        }

        private void OnDestroy()
        {
            _playAgainButton.onClick.RemoveListener(OnPlayAgainClicked);
            _backToLobbyButton.onClick.RemoveListener(OnBackToLobbyClicked);
        }

        private void OnPlayAgainClicked() => PlayAgainClicked?.Invoke();

        private void OnBackToLobbyClicked() => BackToLobbyClicked?.Invoke();

        public void ShowResults(string title, IReadOnlyList<PlayerResultDto> results, Guid me)
        {
            _titleText.text = title;

            var place = 0;
            long previousScore = -1;
            var index = 0;

            UiList.Rebuild(_rowsRoot, _rowPrefab, results, (row, result) =>
            {
                index++;
                if (result.Score != previousScore)
                    place = index; // equal scores share the same place

                previousScore = result.Score;
                row.Bind(place, result, result.PlayerId == me);
            });
        }

        public void SetRematchStatus(string text) => _rematchStatusText.text = text ?? string.Empty;

        public void SetPlayAgainInteractable(bool interactable) => _playAgainButton.interactable = interactable;

        public void SetBackInteractable(bool interactable) => _backToLobbyButton.interactable = interactable;
    }
}
