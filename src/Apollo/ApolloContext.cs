using Apollo.Abstractions;
using Microsoft.Extensions.Primitives;

namespace Apollo;

public class ApolloContext
{
    public IReadOnlyDictionary<string, StringValues> Headers { get; }
    public string Subject => Message.Subject;
    public ApolloData? Data => Message.Data;
    internal ApolloMessage Message { get; }
    internal bool ReplyAvailable => ReplyFunc is not null;
    private Func<byte[] , CancellationToken, Task>? ReplyFunc { get; }
    
    public ApolloContext(ApolloMessage message, Func<byte[] , CancellationToken, Task>? replyFunc = null)
    {
        Message = message;
        ReplyFunc = replyFunc;
        Headers = Message.Headers.AsReadOnly();
    }
    
    internal Task Reply(byte[] response, CancellationToken cancellationToken)
    {
        if (ReplyFunc is null)
            throw new InvalidOperationException("Reply function is not available");
        
        return ReplyFunc(response, cancellationToken);
    }
}