using Apollo.Abstractions;
using Apollo.Configuration;
using Apollo.Internal;

namespace Apollo;

public class ApolloClient //: IPublisher
{
    // eventually used to identify messages from the application
    private readonly Guid instanceId = Guid.NewGuid();
    private readonly ISubscriptionProvider? defaultSubscriptionProvider;
    private readonly IProviderPublisher? providerPublisher;
    private readonly IEndpointProvider? endpointProvider;

    public ApolloClient(
        ISubscriptionProvider? defaultSubscriptionProvider,
        IProviderPublisher? providerPublisher,
        IEndpointProvider? endpointProvider
    )
    {
        this.defaultSubscriptionProvider = defaultSubscriptionProvider;
        this.providerPublisher = providerPublisher;
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

    // execute a handler when a message is received
    public IApolloEndpoint AddHandler(EndpointConfig config, Func<ApolloContext, CancellationToken, Task> handler)
        => new SynchronousEndpoint(
            config,
            config.SubscriptionProvider ?? defaultSubscriptionProvider ?? throw new InvalidOperationException("No SubscriptionProvider provided"),
            handler: handler);

    public IPublisher CreatePublisher(PublishConfig publishConfig)
    {
        publishConfig.ProviderPublisher ??= providerPublisher;
        return new DefaultPublisher(publishConfig);
    }
}