using Apollo.Messaging.Abstractions;

namespace Apollo.Messaging.Replier;

public class NoOpReplier : IReplier
{
    public static NoOpReplier Instance { get; } = new();
    public Task ReplyAsync(object response, CancellationToken cancellationToken) => Task.CompletedTask;
}