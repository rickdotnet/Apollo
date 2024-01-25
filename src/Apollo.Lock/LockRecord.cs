using MessagePack;

namespace Apollo.Lock;

[MessagePackObject]
public record LockRecord
{
    [Key(0)] public string Owner { get; set; }
    [Key(1)] public DateTimeOffset Expiration { get; set; }
}