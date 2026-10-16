using System.Threading.Tasks;
using GameClient.Core;
using GameClient.Lobby.Views;
using GameClient.Networking;
using GameClient.UI;
using VContainer.Unity;

namespace GameClient.Lobby.Presenters
{
    public sealed class AuthPresenter : Presenter, IInitializable
    {
        private readonly AuthView _view;
        private readonly ApiClient _api;
        private readonly GameApi _gameApi;
        private readonly GameHubClient _hub;
        private readonly UserSession _user;
        private readonly LobbyNavigator _navigator;

        public AuthPresenter(AuthView view, ApiClient api, GameApi gameApi, GameHubClient hub,
            UserSession user, LobbyNavigator navigator)
        {
            _view = view;
            _api = api;
            _gameApi = gameApi;
            _hub = hub;
            _user = user;
            _navigator = navigator;
        }

        public void Initialize()
        {
            _view.LoginRequested += OnLogin;
            _view.RegisterRequested += OnRegister;
        }

        public override void Dispose()
        {
            _view.LoginRequested -= OnLogin;
            _view.RegisterRequested -= OnRegister;
            base.Dispose();
        }

        private void OnLogin(string email, string password)
        {
            if (email.Length == 0 || password.Length == 0)
            {
                _view.ShowError("Enter your email and password");
                return;
            }

            SignIn(() => _api.LoginAsync(email, password));
        }

        private void OnRegister(string username, string email, string password)
        {
            if (username.Length is < 3 or > 32)
            {
                _view.ShowError("Username must be 3 to 32 characters long");
                return;
            }

            if (password.Length < 8)
            {
                _view.ShowError("Password must be at least 8 characters long");
                return;
            }

            SignIn(() => _api.RegisterAsync(username, email, password));
        }

        private void SignIn(System.Func<Task> authenticate)
        {
            _view.ShowError(null);
            _view.SetBusy(true);

            Run(async () =>
            {
                try
                {
                    await authenticate();
                    _user.SignIn(await _gameApi.GetMeAsync());
                    await _hub.ConnectAsync();
                    Lifetime.ThrowIfCancellationRequested();
                    _navigator.Show(LobbyScreen.Browser);
                }
                finally
                {
                    if (IsAlive)
                        _view.SetBusy(false);
                }
            }, _view.ShowError);
        }
    }
}
