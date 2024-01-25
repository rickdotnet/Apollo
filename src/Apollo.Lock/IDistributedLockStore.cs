namespace Apollo.Lock;

public interface IDistributedLockStore
{
    DistributedAsyncLock CreateLock(string key, CancellationToken cancellationToken = default);
    //Task AcquireLockAsync(string key, CancellationToken cancellationToken = default);
    //Task ReleaseLockAsync(string key, CancellationToken cancellationToken = default);
}