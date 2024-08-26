using Apollo.Abstractions;
using Apollo.Configuration;
using Microsoft.Extensions.Logging;

namespace Apollo.Providers.ASB;


internal class AsbSubscriptionProvider : ISubscriptionProvider
{
    private readonly ApolloConfig apolloConfig;
    private readonly BusResourceManager resourceManager;
    private readonly ILoggerFactory loggerFactory;

    public AsbSubscriptionProvider(ApolloConfig apolloConfig, BusResourceManager resourceManager, ILoggerFactory loggerFactory)
    {
        this.apolloConfig = apolloConfig;
        this.resourceManager = resourceManager;
        this.loggerFactory = loggerFactory;
    }
    
    public ISubscription AddSubscription(SubscriptionConfig subscriptionConfig, Func<ApolloContext, CancellationToken, Task> handler)
    {
        return new AsbTopicSubscription(
            apolloConfig,
            subscriptionConfig,
            resourceManager,
            loggerFactory.CreateLogger<AsbTopicSubscription>(),
            handler
        );
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