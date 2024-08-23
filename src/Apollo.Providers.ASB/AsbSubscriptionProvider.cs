using Apollo.Abstractions;
using Apollo.Configuration;

namespace Apollo.Providers.ASB;

public class AsbSubscriptionProvider : ISubscriptionProvider
{
    public ISubscription AddSubscription(SubscriptionConfig config, Func<ApolloContext, CancellationToken, Task> handler)
    {
        throw new NotImplementedException();
    }
}

public class AsbSubscription //: ISubscription
{
    // virtual OnDisposeAsync? - for the caller to do some stuff
    public ValueTask DisposeAsync()
    {
        throw new NotImplementedException();
    }
}