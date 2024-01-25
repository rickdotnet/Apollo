using NATS.Client.KeyValueStore;

namespace Apollo.Lock;

public class DistributedLockStoreFactory
{
    private readonly INatsKVContext kvContext;
    private readonly TimeSpan ttl;
    private readonly TimeSpan timeout;

    public DistributedLockStoreFactory(
        INatsKVContext kvContext,
        TimeSpan ttl = default,
        TimeSpan timeout = default)
    {
        this.kvContext = kvContext ?? throw new ArgumentNullException(nameof(kvContext));
        this.ttl = ttl == default ? TimeSpan.FromSeconds(30) : ttl;
        this.timeout = timeout == default ? TimeSpan.FromSeconds(10) : timeout;
    }

    public async Task<IDistributedLockStore> CreateAsync(string owner = "apollo", string bucketName = "apollo", CancellationToken cancellationToken = default)
    {
        // TODO: test the TTL and lease time in the lock store
        
        // TTL not available in NATS KV?
        var config = new NatsKVConfig(bucketName);
        
        var kvStore = await kvContext.CreateStoreAsync(config, cancellationToken);
        return new DistributedLockStore(kvStore, owner, ttl, timeout);
    }
}