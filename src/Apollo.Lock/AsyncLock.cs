// modified from: https://github.com/dotnet/orleans/blob/main/src/Orleans.Core/Async/AsyncLock.cs

namespace Apollo.Lock;

internal class AsyncLock
{
    private readonly SemaphoreSlim semaphore;

    public AsyncLock()
    {
        semaphore = new SemaphoreSlim(1);
    }

    public ValueTask<IAsyncDisposable> LockAsync(CancellationToken cancellationToken = default)
    {
        var wait = semaphore.WaitAsync(cancellationToken);
        return wait.IsCompletedSuccessfully 
            ? new ValueTask<IAsyncDisposable>(new LockReleaser(this)) 
            : LockAsyncAwaited(this, wait);
    }

    public async ValueTask<IAsyncDisposable> LockAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        if (await semaphore.WaitAsync(timeout, cancellationToken).ConfigureAwait(false))
            return new LockReleaser(this);

        throw new TimeoutException("Failed to acquire the lock within the given timeout.");
    }

    private static async ValueTask<IAsyncDisposable> LockAsyncAwaited(AsyncLock self, Task waitTask)
    {
        await waitTask.ConfigureAwait(false);
        return new LockReleaser(self);
    }

    private sealed class LockReleaser : IAsyncDisposable
    {
        private AsyncLock? target;

        internal LockReleaser(AsyncLock target)
        {
            this.target = target;
        }

        public ValueTask DisposeAsync()
        {
            var originalTarget = Interlocked.Exchange(ref target, null);
            originalTarget?.semaphore.Release();
            
            return ValueTask.CompletedTask;
        }
    }
}