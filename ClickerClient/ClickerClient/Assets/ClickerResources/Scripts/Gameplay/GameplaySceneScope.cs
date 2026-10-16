using GameClient.Gameplay.Presenters;
using GameClient.Gameplay.Views;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace GameClient.Gameplay
{
    public class GameplaySceneScope : LifetimeScope
    {
        [SerializeField] private GameplayView _gameplayView;
        [SerializeField] private ResultsView _resultsView;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(_gameplayView);
            builder.RegisterComponent(_resultsView);

            builder.RegisterEntryPoint<GameplayPresenter>();
            builder.RegisterEntryPoint<ResultsPresenter>();
        }
    }
}
