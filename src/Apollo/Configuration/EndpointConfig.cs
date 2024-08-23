using Apollo.Abstractions;

namespace Apollo.Configuration;

public record EndpointConfig
{
    public required string ConsumerName { get; set;}  // instance id?
    public string? Namespace { get; set; } // namespace will prefix endpoint name
    public string? EndpointName { get; init; } // endpoint name or subject must be provided
    public string? EndpointSubject { get; set; } // endpoint name or subject must be provided
    public required bool IsDurable { get; set;}
    public bool CreateMissingResources { get; set; }
    public ISubscriptionProvider? SubscriptionProvider { get; set; }
}
