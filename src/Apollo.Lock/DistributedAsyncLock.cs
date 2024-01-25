namespace Apollo.Lock;

public class DistributedAsyncLock
{
    private readonly DistributedLockStore lockStore;
    private readonly string key;
    private readonly TimeSpan timeout;
    private readonly AsyncLock localLock = new();

    public DistributedAsyncLock(DistributedLockStore lockStore, string key, TimeSpan timeout)
    {
        this.lockStore = lockStore ?? throw new ArgumentNullException(nameof(lockStore));
        this.key = key ?? throw new ArgumentNullException(nameof(key));
        this.timeout = timeout;
    }

    public async Task<IAsyncDisposable> LockAsync(CancellationToken cancellationToken = default)
    {
        // Use a local lock to prevent multiple local tasks from trying to acquire the distributed lock simultaneously
        var releaser = await localLock.LockAsync(timeout, cancellationToken);

        try
        {
            // Attempt to acquire the distributed lock
            await lockStore.AcquireLockAsync(key, cancellationToken);
        }
        catch
        {
            // If something goes wrong, ensure that the local lock is released before throwing the exception
            await releaser.DisposeAsync();
            throw;
        }

        // Return a releaser that will release both the local and distributed locks
        return new DistributedLockReleaser(
            releaser,
            () => lockStore.ReleaseLockAsync(key, cancellationToken));
    }

    private sealed class DistributedLockReleaser : IAsyncDisposable
    {
        private readonly IAsyncDisposable localLock;
        private readonly Func<Task> onDispose;

        internal DistributedLockReleaser(IAsyncDisposable localLock, Func<Task> onDispose)
        {
            this.localLock = localLock;
            this.onDispose = onDispose;
        }

        public async ValueTask DisposeAsync()
        {
            await onDispose.Invoke();
            await localLock.DisposeAsync();
        }
    }
}