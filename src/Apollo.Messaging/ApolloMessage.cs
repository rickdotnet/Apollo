using Apollo.Configuration;
using Apollo.Messaging.Abstractions;
using Apollo.Messaging.Replier;
using Microsoft.Extensions.Primitives;

namespace Apollo.Messaging;

public class ApolloMessage
{
    internal SubscriptionConfig? Config { get; init; }
    public IReplier Replier { get; set; } = NoOpReplier.Instance;
    public string Subject { get; init; } = string.Empty;
    public IDictionary<string, StringValues>? Headers { get; init; }
    public object? Message { get; init; }
    public string? ReplyTo { get; init; }
}