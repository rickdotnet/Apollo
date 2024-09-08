using Apollo.Abstractions;

namespace Apollo.Configuration;

public record EndpointConfig
{
    public string? InstanceId { get; internal set; } // set by apollo
    public string? ConsumerName { get; set;}  // set to ApolloConfig.DefaultConsumerName if not provided
    public string? Namespace { get; set; } // set to ApolloConfig.DefaultNamespace if not provided; always prefixes the subject if provided
    internal Type? EndpointType { get; set; } // set by ApolloClient
    public string? EndpointName { get; init; } // endpoint name or subject must be provided
    public string? EndpointSubject { get; set; } // endpoint name or subject must be provided
    public bool IsDurable { get; set;} 
    public bool CreateMissingResources { get; set; }

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
            EndpointSubject = config.EndpointSubject
        };
    }
}