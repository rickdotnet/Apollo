using MessagePack;
using Microsoft.Extensions.Caching.Distributed;
using NATS.Client.KeyValueStore;

namespace Apollo.Caching;

public class NatsDistributedCache : IDistributedCache
{
    private readonly INatsKVContext kvContext;
    private readonly string bucketName;
    private INatsKVStore? kvStore;
    private readonly SemaphoreSlim initializationSemaphore = new(1, 1);

    public NatsDistributedCache(INatsKVContext kvContext, string bucketName = "apollocache")
    {
        this.kvContext = kvContext ?? throw new ArgumentNullException(nameof(kvContext));
        this.bucketName = bucketName;
    }

    private async ValueTask<INatsKVStore> GetStoreAsync(CancellationToken token = default)
    {
        if (kvStore != null) return kvStore;
        
        await initializationSemaphore.WaitAsync(token);
        try
        {
            kvStore ??= await kvContext.CreateStoreAsync(new NatsKVConfig(bucketName), token);
        }
        finally
        {
            initializationSemaphore.Release();
        }

        return kvStore;
    }

    public byte[]? Get(string key)
        => GetAsync(key).GetAwaiter().GetResult();


    public async Task<byte[]?> GetAsync(string key, CancellationToken token = default)
    {
        var store = await GetStoreAsync(token);

        var entry = await store.GetEntryAsync<byte[]>(key, cancellationToken: token);

        if (entry.Value is null)
            return null;

        var cacheItem = DeserializeFromBytes<CacheItem>(entry.Value);

        if (DateTimeOffset.UtcNow <= cacheItem.Expiration)
            return cacheItem.Value;

        await store.DeleteAsync(key, cancellationToken: token);
        return null;
    }

    // no-op
    public void Refresh(string key) { }
    public Task RefreshAsync(string key, CancellationToken token = default)
        => Task.CompletedTask;

    public void Remove(string key)
        => RemoveAsync(key).GetAwaiter().GetResult();

    public async Task RemoveAsync(string key, CancellationToken token = default)
    {
        var store = await GetStoreAsync(token);
        await store.DeleteAsync(key, cancellationToken: token);
    }

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        => SetAsync(key, value, options).GetAwaiter().GetResult();

    public async Task SetAsync(
        string key,
        byte[] value,
        DistributedCacheEntryOptions options,
        CancellationToken token = default
    )
    {
        var store = await GetStoreAsync(token);

        var cacheItem = new CacheItem
        {
            Value = value,
            Expiration = DateTimeOffset.UtcNow.Add(options.AbsoluteExpirationRelativeToNow ??
                                                   options.SlidingExpiration ?? TimeSpan.FromMinutes(20))
        };

        var itemBytes = SerializeToBytes(cacheItem);
        await store.PutAsync(key, itemBytes, cancellationToken: token);
    }

    private static byte[] SerializeToBytes<T>(T obj) 
        => MessagePackSerializer.Serialize(obj);

    private static T DeserializeFromBytes<T>(byte[] bytes) 
        => MessagePackSerializer.Deserialize<T>(bytes);
}