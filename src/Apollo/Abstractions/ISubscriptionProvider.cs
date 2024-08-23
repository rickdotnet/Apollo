using Apollo.Configuration;

namespace Apollo.Abstractions;

public interface ISubscription //: IAsyncDisposable
{
    // TODO: need to consider moving this down here
    //       then, we could either start the handler
    //       or return an AsyncEnumerable that they can handle
    //Task SubscribeAsync(Func<ApolloContext, CancellationToken, Task> handler, CancellationToken cancellationToken);
    Task SubscribeAsync(CancellationToken cancellationToken);
    // ValueTask DisposeAsync();
}
public interface ISubscriptionProvider
{
    ISubscription AddSubscription(SubscriptionConfig config, Func<ApolloContext, CancellationToken, Task> handler);
    
    // in line with above, do we pass the handler with the config and let the subscription figure it out?
    // if the sub doesn't have a handler when starting, it can drop a fat exception to let people know
    // TODO: put some thought into these two
    //ISubscription AddSubscription(SubscriptionConfig config);
}

