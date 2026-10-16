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
        private readonly CancellationTokenSource _lifetimeSource = new();
        private readonly CancellationToken _lifetime;
        private bool _disposed;

        protected Presenter() => _lifetime = _lifetimeSource.Token;

        // The token is cached: it stays usable after the source is disposed
        protected CancellationToken Lifetime => _lifetime;
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

        public virtual void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _lifetimeSource.Cancel();
            _lifetimeSource.Dispose();
        }
    }
}
