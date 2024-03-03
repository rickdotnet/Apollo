using System.Collections.Concurrent;
using NATS.Client.KeyValueStore;
using MessagePack;

namespace Apollo.Lock;

public class DistributedLockStore : IDistributedLockStore
{
    public TimeSpan MaxLeaseTime { get; }

    private readonly INatsKVStore kvStore;
    private readonly ConcurrentDictionary<string, DistributedAsyncLock> locks = new();
    private readonly string owner;
    private readonly TimeSpan timeout;
    private readonly TimeSpan acquireDelay = TimeSpan.FromMilliseconds(500);

    public DistributedLockStore(INatsKVStore kvStore, string owner, TimeSpan maxLeaseTime, TimeSpan timeout)
    {
        this.kvStore = kvStore ?? throw new ArgumentNullException(nameof(kvStore));
        this.owner = owner;
        this.MaxLeaseTime = maxLeaseTime;
        this.timeout = timeout;
    }

    public DistributedAsyncLock CreateLock(string key, CancellationToken cancellationToken = default)
    {
        return locks.GetOrAdd(key, k => new DistributedAsyncLock(this, k, timeout));
    }

    internal async Task AcquireLockAsync(string key, TimeSpan leaseTime, CancellationToken cancellationToken = default)
    {
        var time = DateTimeOffset.UtcNow.Add(timeout);
        while (DateTimeOffset.UtcNow < time && !cancellationToken.IsCancellationRequested)
        {
            LockRecord? lockRecord = null;
            ulong lastRevision = 0;
            ulong newRevision = 0;
            try
            {
                var entry = await kvStore.GetEntryAsync<byte[]>(key, cancellationToken: cancellationToken);
                lastRevision = entry.Revision;
                lockRecord = entry.Value is null ? null : DeserializeFromBytes<LockRecord>(entry.Value);
            }
            catch (Exception ex) when (ex is NatsKVKeyNotFoundException or NatsKVKeyDeletedException)
            {
                // ignore since this is expected
            }

            if (lockRecord is null
                || lockRecord.Owner == owner
                || lockRecord.Expiration < DateTimeOffset.UtcNow)
            {
                var newItem = new LockRecord
                {
                    Owner = owner,
                    Expiration = DateTimeOffset.UtcNow.Add(leaseTime)
                };

                // either we're the owner or the lock is expired
                // not sure how often this path will be hit
                if (lockRecord != null)
                {
                    Console.WriteLine($"Lock record: {lockRecord.Owner} {lockRecord.Expiration}");
                    newRevision = await kvStore.UpdateAsync(key, SerializeToBytes(newItem), lastRevision,
                        cancellationToken: cancellationToken);
                   // return;
                }
                else
                {
                    newRevision = await kvStore.PutAsync(key, SerializeToBytes(newItem),
                        cancellationToken: cancellationToken);
                }

                // pay a small fee to validate the lock didn't change
                await Task.Delay(acquireDelay, cancellationToken);

                var entry = await kvStore.GetEntryAsync<byte[]>(key, cancellationToken: cancellationToken);
                
                // we're gucci
                if (entry.Revision == newRevision)
                    return;
                //throw new InvalidOperationException("Failed to acquire lock");
            }
            else
            {
                if (DateTimeOffset.UtcNow.Add(acquireDelay) < time)
                    await Task.Delay(acquireDelay, cancellationToken);
            }
        }

        throw new TimeoutException($"Failed to acquire lock for key {key} within {timeout}");
    }

    internal async Task ReleaseLockAsync(string key, CancellationToken cancellationToken = default)
    {
        var entry = await kvStore.GetEntryAsync<byte[]>(key, cancellationToken: cancellationToken);
        var cacheItem = entry.Value is null ? null : DeserializeFromBytes<LockRecord>(entry.Value);

        if (cacheItem != null)

        {
            if (cacheItem.Owner == owner || cacheItem.Expiration < DateTimeOffset.UtcNow)
            {
                await kvStore.DeleteAsync(key, cancellationToken: cancellationToken).AsTask();
                //await kvStore.UpdateAsync(key, SerializeToBytes(LockRecord.Empty), revision, cancellationToken: cancellationToken);
            }
            else
            {
                throw new InvalidOperationException(
                    $"Cannot release lock for key {key} because the lock is owned by {cacheItem.Owner}");
            }
        }
    }

    private static byte[] SerializeToBytes<T>(T obj)
        => MessagePackSerializer.Serialize(obj);

    private static T DeserializeFromBytes<T>(byte[] bytes)
        => MessagePackSerializer.Deserialize<T>(bytes);
}