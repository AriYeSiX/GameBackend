using GameClient.Lobby.Presenters;
using GameClient.Lobby.Views;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace GameClient.Lobby
{
    /// <summary>Lobby scene scope. Its parent is the root BootstrapScope (via VContainerSettings).</summary>
    public class LobbySceneScope : LifetimeScope
    {
        [SerializeField] private AuthView _authView;
        [SerializeField] private LobbyBrowserView _browserView;
        [SerializeField] private LobbyRoomView _roomView;
        [SerializeField] private LeaderboardView _leaderboardView;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(_authView);
            builder.RegisterComponent(_browserView);
            builder.RegisterComponent(_roomView);
            builder.RegisterComponent(_leaderboardView);

            builder.Register<LobbyNavigator>(Lifetime.Scoped);

            builder.RegisterEntryPoint<AuthPresenter>();
            builder.RegisterEntryPoint<LobbyBrowserPresenter>();
            builder.RegisterEntryPoint<LobbyRoomPresenter>();
            builder.RegisterEntryPoint<LeaderboardPresenter>();
            builder.RegisterEntryPoint<LobbyEntryPresenter>();
        }
    }
}
