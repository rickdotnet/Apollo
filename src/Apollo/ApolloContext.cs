using Apollo.Abstractions;

namespace Apollo;

public class ApolloContext
{
    public ApolloMessage Message { get; }
    public bool ReplyAvailable => ReplyFunc is not null;
    private Func<byte[] , CancellationToken, Task>? ReplyFunc { get; }
    
    public ApolloContext(ApolloMessage message, Func<byte[] , CancellationToken, Task>? replyFunc = null)
    {
        Message = message;
        ReplyFunc = replyFunc;
    }
    
    public Task ReplyAsync(byte[] response, CancellationToken cancellationToken)
    {
        if (ReplyFunc is null)
            throw new InvalidOperationException("Reply function is not available");
        
        return ReplyFunc(response, cancellationToken);
    }
}