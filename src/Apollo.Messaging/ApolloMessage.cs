using Apollo.Configuration;
using Apollo.Messaging.Abstractions;
using Apollo.Messaging.Replier;
using Microsoft.Extensions.Primitives;

namespace Apollo.Messaging;

public class ApolloMessage
{
    internal SubscriptionConfig? Config { get; set; }
    public IReplier Replier { get; set; } = NoOpReplier.Instance;
    public string Subject { get; set; } = string.Empty;
    public IDictionary<string, StringValues>? Headers { get; set; }
    public object? Message { get; set; }
    public string? ReplyTo { get; set; }
}