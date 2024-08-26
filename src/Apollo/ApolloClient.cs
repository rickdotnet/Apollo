using Apollo.Abstractions;
using Apollo.Configuration;
using Apollo.Internal;
using Apollo.Providers.Memory;

namespace Apollo;

public class ApolloClient
{
    private readonly ISubscriptionProvider? defaultSubscriptionProvider;
    private readonly ApolloConfig apolloConfig;
    private readonly IProviderPublisher? providerPublisher;
    private readonly IEndpointProvider? endpointProvider;

    public ApolloClient(
        ApolloConfig apolloConfig,
        ISubscriptionProvider? defaultSubscriptionProvider = null,
        IProviderPublisher? providerPublisher = null,
        IEndpointProvider? endpointProvider = null
    )
    {
        this.defaultSubscriptionProvider = defaultSubscriptionProvider ?? InMemoryProvider.Instance;
        this.apolloConfig = apolloConfig;
        this.providerPublisher = providerPublisher;

        // the default subscription provider might double as a publisher
        if (this.providerPublisher is null
            && this.defaultSubscriptionProvider is IProviderPublisher provider)
        {
            this.providerPublisher = provider;
        }

        this.endpointProvider = endpointProvider;
    }

    public IApolloEndpoint AddEndpoint<T>(EndpointConfig endpointConfig)
    {
        var config = SetEndpointDefaults(endpointConfig, typeof(T));
        if (config.AsyncMode)
            throw new NotImplementedException("Async mode is not implemented yet");

        return new SynchronousEndpoint(config);
    }

    public IApolloEndpoint AddHandler(EndpointConfig endpointConfig,
        Func<ApolloContext, CancellationToken, Task> handler)
    {
        var config = SetEndpointDefaults(endpointConfig);
        if (config.AsyncMode)
            throw new NotImplementedException("Async mode is not implemented yet");
        
        return new SynchronousEndpoint(config, handler: handler);
    }

    public IPublisher CreatePublisher(EndpointConfig endpointConfig)
        => CreatePublisher(endpointConfig.ToPublishConfig());

    public IPublisher CreatePublisher(PublishConfig publishConfig)
    {
        publishConfig.ProviderPublisher ??= providerPublisher;
        return new DefaultPublisher(publishConfig);
    }
    
    private EndpointConfig SetEndpointDefaults(EndpointConfig config, Type? endpointType = null) 
        => config with
        {
            InstanceId = config.InstanceId ?? apolloConfig.InstanceId,
            ConsumerName = config.ConsumerName 
                           ?? apolloConfig.DefaultConsumerName 
                           ?? throw new Exception("ConsumerName is required."),
            Namespace = config.Namespace ?? apolloConfig.DefaultNamespace,
            EndpointType = config.EndpointType ?? endpointType,
            SubscriptionProvider = config.SubscriptionProvider ?? defaultSubscriptionProvider,
            EndpointProvider = config.EndpointProvider ?? endpointProvider
        };
}