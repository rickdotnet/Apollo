using Apollo.Abstractions;

namespace Apollo.Configuration;

public record EndpointConfig
{
    public required string ConsumerName { get; set;}  // instance id?
    public string? Namespace { get; set; } // namespace will prefix endpoint name
    public string? EndpointName { get; init; } // endpoint name or subject must be provided
    public string? EndpointSubject { get; set; } // endpoint name or subject must be provided
    public bool IsDurable { get; set;}
    public bool CreateMissingResources { get; set; }
    public ISubscriptionProvider? SubscriptionProvider { get; set; }
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