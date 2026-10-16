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

        // Prebuilt strings: Tick runs every frame and should not allocate
        private static readonly string[] StatusTexts =
        {
            "Waiting for other players.",
            "Waiting for other players..",
            "Waiting for other players..."
        };

        private int _shownStatus = -1;

        public void Tick()
        {
            var status = (int)(Time.unscaledTime * 2) % StatusTexts.Length;
            if (status == _shownStatus)
                return;

            _shownStatus = status;
            _view.SetStatus(StatusTexts[status]);
        }
    }
}
