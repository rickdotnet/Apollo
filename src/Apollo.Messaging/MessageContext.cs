using Apollo.Messaging.Replier;

namespace Apollo.Messaging;

public record MessageContext
{
    public IDictionary<string,string> Headers { get; init; } = new Dictionary<string, string>();
    public required string Subject { get; init; }
    public required string Source { get; init; }
    public string? ReplyTo { get; init; }
    internal object? Message { get; init; }
    internal IReplier Replier { get; init; } = NoOpReplier.Instance;
}

public static class MessageContextExtensions
{
   public static Task ReplyAsync(this MessageContext messageContext, object response, CancellationToken cancellationToken = default)
   {
       ArgumentNullException.ThrowIfNull(response, nameof(response));
       return messageContext.Replier.ReplyAsync(response, cancellationToken);
   }
}