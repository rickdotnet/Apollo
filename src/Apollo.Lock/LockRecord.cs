using MessagePack;

namespace Apollo.Lock;

[MessagePackObject]
public record LockRecord
{
    internal static LockRecord Empty = new() { Owner = string.Empty, Expiration = DateTimeOffset.MinValue };
    [Key(0)] public required string Owner { get; set; }
    [Key(1)] public DateTimeOffset Expiration { get; set; }
}