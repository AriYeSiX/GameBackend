using GameClient.Core;
using GameClient.UI;
using UnityEngine;
using VContainer.Unity;

namespace GameClient.Loading
{
    /// <summary>
    /// The loading screen only displays the state. Loading itself and waiting
    /// for the other players are handled by GameFlow.
    /// </summary>
    public sealed class LoadingPresenter : Presenter, IStartable, ITickable
    {
        private readonly LoadingView _view;
        private readonly MatchSession _match;

        public LoadingPresenter(LoadingView view, MatchSession match)
        {
            _view = view;
            _match = match;
        }

        public void Start() => _view.SetPlayers(_match.Players);

        public void Tick()
        {
            var dots = new string('.', 1 + (int)(Time.unscaledTime * 2) % 3);
            _view.SetStatus($"Waiting for other players{dots}");
        }
    }
}
