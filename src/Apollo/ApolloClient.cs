using Apollo.Abstractions;
using Apollo.Configuration;
using Apollo.Internal;
using Apollo.Providers.Memory;

namespace Apollo;

public class ApolloClient //: IPublisher
{
    // eventually used to identify messages from the application
    private readonly Guid instanceId = Guid.NewGuid();
    private readonly ISubscriptionProvider? defaultSubscriptionProvider;
    private readonly IProviderPublisher? providerPublisher;
    private readonly IEndpointProvider? endpointProvider;

    public ApolloClient(
        ISubscriptionProvider? defaultSubscriptionProvider = null,
        IProviderPublisher? providerPublisher = null,
        IEndpointProvider? endpointProvider = null
    )
    {
        this.defaultSubscriptionProvider = defaultSubscriptionProvider ?? InMemoryProvider.Instance;
        this.providerPublisher = providerPublisher;

        // the default subscription provider might double as a publisher
        if (this.providerPublisher is null 
            && this.defaultSubscriptionProvider is IProviderPublisher provider)
        {
            this.providerPublisher = provider;
        }

        this.endpointProvider = endpointProvider;
    }

    // add an endpoint with handlers that are invoked when a message is received
    public IApolloEndpoint AddEndpoint<T>(EndpointConfig config)
    {
        // if config.AsyncMode is false (default)
        return new SynchronousEndpoint(
            config,
            config.SubscriptionProvider ?? defaultSubscriptionProvider ?? throw new InvalidOperationException("No SubscriptionProvider provided"),
            endpointProvider ?? throw new InvalidOperationException("No EndpointProvider provided"),
            endpointType: typeof(T)
        );

        // if config.AsyncMode is true
        // return new AsynchronousEndpoint()
    }

    public IApolloEndpoint AddHandler(EndpointConfig config, Func<ApolloContext, CancellationToken, Task> handler)
        => new SynchronousEndpoint(
            config,
            config.SubscriptionProvider ?? defaultSubscriptionProvider ?? throw new InvalidOperationException("No SubscriptionProvider provided"),
            handler: handler);

    public IPublisher CreatePublisher(EndpointConfig endpointConfig)
        => CreatePublisher(endpointConfig.ToPublishConfig());
    
    public IPublisher CreatePublisher(PublishConfig publishConfig)
    {
        publishConfig.ProviderPublisher ??= providerPublisher;
        return new DefaultPublisher(publishConfig);
    }
}