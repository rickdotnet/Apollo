using Microsoft.Extensions.Primitives;

namespace Apollo.Nats;

public class NatsMessage
{
    internal NatsSubscriptionConfig Config { get; init; }
    public string Subject { get; init; } = string.Empty;
    public IDictionary<string, StringValues>? Headers { get; init; }
    public object? Message { get; init; }
    public string? ReplyTo { get; init; }
}