using System.Collections.Concurrent;
using NATS.Client.KeyValueStore;
using MessagePack;

namespace Apollo.Lock;

public class DistributedLockStore : IDistributedLockStore
{
    private readonly INatsKVStore kvStore;
    private readonly ConcurrentDictionary<string, DistributedAsyncLock> locks = new();
    private readonly string owner;
    private readonly TimeSpan leaseTime;
    private readonly TimeSpan timeout;
    private readonly TimeSpan acquireDelay = TimeSpan.FromMilliseconds(500);

    public DistributedLockStore(INatsKVStore kvStore, string owner, TimeSpan leaseTime, TimeSpan timeout)
    {
        this.kvStore = kvStore ?? throw new ArgumentNullException(nameof(kvStore));
        this.owner = owner;
        this.leaseTime = leaseTime;
        this.timeout = timeout;
    }

    public DistributedAsyncLock CreateLock(string key, CancellationToken cancellationToken = default)
    {
        return locks.GetOrAdd(key, k => new DistributedAsyncLock(this, k, timeout));
    }

    internal async Task AcquireLockAsync(string key, CancellationToken cancellationToken = default)
    {
        var time = DateTimeOffset.UtcNow.Add(timeout);
        while (DateTimeOffset.UtcNow < time && !cancellationToken.IsCancellationRequested)
        {
            LockRecord? lockRecord = null;
            try
            {
                var entry = await kvStore.GetEntryAsync<byte[]>(key, cancellationToken: cancellationToken);
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

                await kvStore.PutAsync(key, SerializeToBytes(newItem), cancellationToken: cancellationToken);
                return;
            }

            if (DateTimeOffset.UtcNow.Add(acquireDelay) < time)
                await Task.Delay(acquireDelay, cancellationToken);
        }

        throw new TimeoutException($"Failed to acquire lock for key {key} within {timeout}");
    }

    internal async Task ReleaseLockAsync(string key, CancellationToken cancellationToken = default)
    {
        var entry = await kvStore.GetEntryAsync<byte[]>(key, cancellationToken: cancellationToken);
        var cacheItem = entry.Value is null ? null : DeserializeFromBytes<LockRecord>(entry.Value);

        if (cacheItem != null)

        {
            if (cacheItem.Owner == owner)
                await kvStore.DeleteAsync(key, cancellationToken: cancellationToken).AsTask();
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