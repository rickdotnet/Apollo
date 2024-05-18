using Apollo.Messaging.Abstractions;
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
    internal Type? ReplierType { get; set; }

    internal MessageContext WithSubject(string subject) => this with { Subject = subject };
    internal MessageContext WithMessage(object message) => this with { Message = message };
    internal MessageContext WithSource(string source) => this with { Source = source };
    internal MessageContext WithReplyTo(string replyTo) => this with { ReplyTo = replyTo };
    internal MessageContext WithReplier(IReplier replier) => this with { Replier = replier };
}

public static class MessageContextExtensions
{
   public static Task ReplyAsync(this MessageContext messageContext, object response, CancellationToken cancellationToken = default)
   {
       ArgumentNullException.ThrowIfNull(response, nameof(response));
       return messageContext.Replier.ReplyAsync(response, cancellationToken);
   }
}