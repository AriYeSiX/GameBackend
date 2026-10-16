using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace GameClient.UI
{
    /// <summary>
    /// Base class for scene presenters. VContainer calls Dispose when the scene is unloaded;
    /// after that Lifetime is cancelled, so async operations do not touch destroyed views.
    /// </summary>
    public abstract class Presenter : IDisposable
    {
        private readonly CancellationTokenSource _lifetime = new();

        protected CancellationToken Lifetime => _lifetime.Token;
        protected bool IsAlive => !_lifetime.IsCancellationRequested;

        /// <summary>Runs an async operation from a button or event handler.</summary>
        protected async void Run(Func<Task> action, Action<string> onError = null)
        {
            try
            {
                await action();
            }
            catch (OperationCanceledException)
            {
                // the scene was unloaded while the operation was running
            }
            catch (Exception ex)
            {
                if (!IsAlive)
                    return;

                if (onError != null)
                    onError(ErrorText.From(ex));
                else
                    Debug.LogException(ex);
            }
        }

        public virtual void Dispose() => _lifetime.Cancel();
    }
}
