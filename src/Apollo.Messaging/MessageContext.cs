using Apollo.Messaging.Replier;

namespace Apollo.Messaging;

public record MessageContext
{
    public IDictionary<string,string> Headers { get; init; } = new Dictionary<string, string>();
    public required string Subject { get; init; }
    public object? Message { get; init; }
    public required string Source { get; init; }
    public string? ReplyTo { get; init; }
    public IReplier Replier { get; init; } = NoOpReplier.Instance;

    public MessageContext WithSubject(string subject) => this with { Subject = subject };

    public MessageContext WithMessage(object message) => this with { Message = message };

    public MessageContext WithSource(string source) => this with { Source = source };
    
    public MessageContext WithReplyTo(string replyTo) => this with { ReplyTo = replyTo };

    public MessageContext WithReplier(IReplier replier) => this with { Replier = replier };
}
