namespace Apollo.Messaging.Abstractions;

public interface IReplier
{
    Task ReplyAsync(object response, CancellationToken cancellationToken);
}