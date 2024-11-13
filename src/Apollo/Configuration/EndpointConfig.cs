using Apollo.Abstractions;

namespace Apollo.Configuration;

public record EndpointConfig
{
    /// <summary>
    /// Defaults to ApolloConfig.InstanceId
    /// </summary>
    public string? InstanceId { get; internal set; }
    
    /// <summary>
    /// Consumer name is required for durable subscriptions and load balancing 
    /// </summary>
    public string? ConsumerName { get; set;}
    
    /// <summary>
    /// Optional namespace for isolation. Defaults to ApolloConfig.DefaultNamespace
    /// </summary>
    public string? Namespace { get; set; }
    
    /// <summary>
    /// Set by ApolloClient
    /// </summary>
    internal Type? EndpointType { get; set; }
    
    /// <summary>
    /// Used to create a unique endpoint name if no subject is provided
    /// </summary>
    public string? EndpointName { get; init; }
    
    /// <summary>
    /// The subject to use for the endpoint. If not provided, the endpoint name will be slugified. If neither are provided, the namespace will be used.
    /// </summary>
    public string? Subject { get; set; } // endpoint name or subject must be provided
    
    /// <summary>
    /// Indicates to the subscription provider that the endpoint should be created as a durable subscription
    /// </summary>
    public bool IsDurable { get; set;}

    /// <summary>
    /// Provides internal access to backing field for CreateMissingResources
    /// </summary>
    internal bool? InternalCreateMissingResources { get; set; } = null;

    /// <summary>
    /// Provides create/update/delete permissions for resources
    /// </summary>
    public bool CreateMissingResources
    {
        get => InternalCreateMissingResources ?? false;
        set => InternalCreateMissingResources = value;
    }
    
    internal AckStrategy? InternalAckStrategy { get; set; }
    
    /// <summary>
    /// For Destrugter
    /// </summary>
    public AckStrategy AckStrategy
    {
        get => InternalAckStrategy ?? AckStrategy.Default;
        set => InternalAckStrategy = value;
    }

    /// <summary>
    /// Not implemented yet
    /// </summary>
    public bool AsyncMode => false;
    
    /// <summary>
    /// Optionally override the DI subscription provider
    /// </summary>
    public ISubscriptionProvider? SubscriptionProvider { get; set; }
    
    /// <summary>
    /// Optionally override the DI endpoint provider
    /// </summary>
    public IEndpointProvider? EndpointProvider { get; set; }
}

public static class EndpointConfigExtensions
{
    public static PublishConfig ToPublishConfig(this EndpointConfig config)
    {
        return new PublishConfig
        {
            Namespace = config.Namespace,
            EndpointName = config.EndpointName,
            Subject = config.Subject
        };
    }
}