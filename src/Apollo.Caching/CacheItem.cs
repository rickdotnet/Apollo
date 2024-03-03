using MessagePack;

namespace Apollo.Caching;

[MessagePackObject]
public class CacheItem
{
    [Key(0)] public byte[] Value { get; set; }
    [Key(1)] public DateTimeOffset Expiration { get; set; }
}