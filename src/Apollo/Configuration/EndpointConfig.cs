using Apollo.Abstractions;

namespace Apollo.Configuration;

public record EndpointConfig
{
    public string? ConsumerName { get; set;}  // set to ApolloConfig.DefaultConsumerName if not provided
    public string? Namespace { get; set; } // set to ApolloConfig.DefaultNamespace if not provided; always prefixes the subject if provided
    public string? EndpointName { get; init; } // endpoint name or subject must be provided
    public string? EndpointSubject { get; set; } // endpoint name or subject must be provided
    public bool IsDurable { get; set;} 
    public bool CreateMissingResources { get; set; }
    public ISubscriptionProvider? SubscriptionProvider { get; set; } // optionally override the DI subscription provider
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