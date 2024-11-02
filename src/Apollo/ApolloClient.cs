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
        => AddEndpoint(typeof(T), endpointConfig);
    
    public IApolloEndpoint AddEndpoint(Type endpointType, EndpointConfig endpointConfig)
    {
        var config = SetEndpointDefaults(endpointConfig, endpointType);
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
        var config = SetPublishDefaults(publishConfig);
        return new DefaultPublisher(config);
    }
    
    private PublishConfig SetPublishDefaults(PublishConfig config, Type? endpointType = null) 
        => config with
        {
            Namespace = config.Namespace ?? apolloConfig.DefaultNamespace,
            ProviderPublisher = config.ProviderPublisher ?? providerPublisher
        };
    
    private EndpointConfig SetEndpointDefaults(EndpointConfig config, Type? endpointType = null) 
        => config with
        {
            InstanceId = config.InstanceId ?? apolloConfig.InstanceId,
            ConsumerName = config.ConsumerName 
                           ?? apolloConfig.DefaultConsumerName 
                           ?? throw new Exception("ConsumerName is required."),
            CreateMissingResources = config.InternalCreateMissingResources.HasValue ? config.CreateMissingResources : apolloConfig.CreateMissingResources,
            Namespace = config.Namespace ?? apolloConfig.DefaultNamespace,
            EndpointType = config.EndpointType ?? endpointType,
            SubscriptionProvider = config.SubscriptionProvider ?? defaultSubscriptionProvider,
            EndpointProvider = config.EndpointProvider ?? endpointProvider
        };
}