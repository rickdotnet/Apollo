using NATS.Client.KeyValueStore;

namespace Apollo.Lock;

public class DistributedLockStoreFactory
{
    private readonly INatsKVContext kvContext;
    private readonly TimeSpan maxTtl;
    private readonly TimeSpan timeout;

    public DistributedLockStoreFactory(
        INatsKVContext kvContext,
        TimeSpan maxTtl = default,
        TimeSpan timeout = default)
    {
        this.kvContext = kvContext ?? throw new ArgumentNullException(nameof(kvContext));
        this.maxTtl = maxTtl == default ? TimeSpan.FromSeconds(30) : maxTtl;
        this.timeout = timeout == default ? TimeSpan.FromSeconds(30) : timeout;
    }

    public async Task<IDistributedLockStore> CreateAsync(string owner = "apollo", string bucketName = "apollo", CancellationToken cancellationToken = default)
    {
        // TODO: test the TTL and lease time in the lock store
        
        var config = new NatsKVConfig(bucketName)
        {
            Description = "Apollo Distributed Lock Store",
            History = 1,

            // TODO: make this apparent to the user
            //       this sets a hard limit at the server level
            //       need to make sure that updates reset this
            //MaxAge = maxTtl, // can't set until DuplicateWindow error PR gets merged
            Storage = NatsKVStorageType.Memory,
        };
        
        var kvStore = await kvContext.CreateStoreAsync(config, cancellationToken);
        return new DistributedLockStore(kvStore, owner, maxTtl, timeout);
    }
}