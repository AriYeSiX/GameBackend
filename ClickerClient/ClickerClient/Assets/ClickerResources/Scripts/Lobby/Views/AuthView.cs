using System;
using GameClient.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameClient.Lobby.Views
{
    public class AuthView : ScreenView
    {
        [SerializeField] private TMP_InputField _usernameInput; // only needed for registration
        [SerializeField] private TMP_InputField _emailInput;
        [SerializeField] private TMP_InputField _passwordInput;
        [SerializeField] private Button _loginButton;
        [SerializeField] private Button _registerButton;
        [SerializeField] private TMP_Text _errorText;

        public event Action<string, string> LoginRequested;            // email, password
        public event Action<string, string, string> RegisterRequested; // username, email, password

        private void Awake()
        {
            _loginButton.onClick.AddListener(() =>
                LoginRequested?.Invoke(_emailInput.text.Trim(), _passwordInput.text));

            _registerButton.onClick.AddListener(() =>
                RegisterRequested?.Invoke(_usernameInput.text.Trim(), _emailInput.text.Trim(), _passwordInput.text));
        }

        private void OnDestroy()
        {
            _loginButton.onClick.RemoveAllListeners();
            _registerButton.onClick.RemoveAllListeners();
        }

        public void SetBusy(bool busy)
        {
            _loginButton.interactable = !busy;
            _registerButton.interactable = !busy;
        }

        public void ShowError(string message) => _errorText.text = message ?? string.Empty;
    }
}
