using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace GameClient.Loading
{
    public class LoadingSceneScope : LifetimeScope
    {
        [SerializeField] private LoadingView _view;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(_view);
            builder.RegisterEntryPoint<LoadingPresenter>();
        }
    }
}
