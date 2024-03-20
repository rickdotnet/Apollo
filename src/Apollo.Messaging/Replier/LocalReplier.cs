namespace Apollo.Messaging.Replier;

public class LocalReplier : IReplier
{
    private readonly TaskCompletionSource<object> responseSource = new();

    public Task<object> ResponseTask => responseSource.Task;

    public Task ReplyAsync(object response, CancellationToken cancellationToken)
    {
        responseSource.TrySetResult(response);
        return Task.CompletedTask;
    }
}