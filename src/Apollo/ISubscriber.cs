namespace Apollo.NATS;

public interface ISubscriber
{
    Task SubscribeAsync(Func<ApolloMessage, CancellationToken, Task> handler);
}