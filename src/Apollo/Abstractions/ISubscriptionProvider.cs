using Apollo.Configuration;

namespace Apollo.Abstractions;

public interface ISubscription //: IAsyncDisposable
{
    Task SubscribeAsync(CancellationToken cancellationToken);
}
public interface ISubscriptionProvider
{
    ISubscription AddSubscription(SubscriptionConfig config, Func<ApolloContext, CancellationToken, Task> handler);
}

