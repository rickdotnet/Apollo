namespace Apollo.Messaging.Replier;

public interface IReplier
{
    Task ReplyAsync(object response, CancellationToken cancellationToken);
}