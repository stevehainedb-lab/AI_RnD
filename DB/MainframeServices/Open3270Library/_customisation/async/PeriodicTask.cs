using System;
using System.Threading;
using System.Threading.Tasks;

namespace Open3270.Library
{
    /// <summary>
    /// Minimal modern periodic async runner using PeriodicTimer.
    /// Construct it to start; Dispose/DisposeAsync to stop.
    /// </summary>
    public sealed class PeriodicTask : IAsyncDisposable, IDisposable
    {
        private readonly PeriodicTimer _timer;
        private readonly CancellationTokenSource _cts;
        private readonly Task _loop;
        private readonly Func<CancellationToken, ValueTask> _callback;

        public PeriodicTask(
            TimeSpan interval,
            Func<CancellationToken, ValueTask> callback,
            bool runImmediately = false,
            CancellationToken cancellation = default)
        {
			ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(interval, TimeSpan.Zero);
			_callback = callback ?? throw new ArgumentNullException(nameof(callback));

            _timer = new PeriodicTimer(interval);
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellation);

            _loop = RunAsync(runImmediately, _cts.Token);
        }

        private async Task RunAsync(bool runImmediately, CancellationToken token)
        {
            try
            {
                if (runImmediately)
                    await _callback(token).ConfigureAwait(false);

                while (await _timer.WaitForNextTickAsync(token).ConfigureAwait(false))
                    await _callback(token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // normal shutdown
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            _timer.Dispose();
            _cts.Dispose();
            // fire-and-forget; caller can use DisposeAsync to await shutdown if needed
        }

        public async ValueTask DisposeAsync()
        {
            await _cts.CancelAsync();
            _timer.Dispose();
            try { await _loop.ConfigureAwait(false); }
            catch (OperationCanceledException) { /* expected */ }
            _cts.Dispose();
        }
    }
}
