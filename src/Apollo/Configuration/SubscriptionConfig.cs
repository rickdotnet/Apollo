using Apollo.Internal;

namespace Apollo.Configuration;

public record SubscriptionConfig
{
    public required string ConsumerName { get; set; }
    public string? Namespace { get; set; }
    public string? EndpointName { get; init; }
    public string? EndpointSubject { get; set; }
    public Type? EndpointType { get; set; }
    // not used yet, but might be used for serialization
    public required Type[] MessageTypes { get; init; } = [];
    public required bool IsDurable { get; set; }
    public bool CreateMissingResources { get; set; }

    public static SubscriptionConfig ForEndpoint(EndpointConfig endpointConfig, Type? endpointType = null, Type[]? messageTypes = null)
    {
        return new SubscriptionConfig
        {
            ConsumerName = endpointConfig.ConsumerName,
            Namespace = endpointConfig.Namespace,
            EndpointName = endpointConfig.EndpointName,
            EndpointSubject = endpointConfig.EndpointSubject,
            IsDurable = endpointConfig.IsDurable,
            CreateMissingResources = endpointConfig.CreateMissingResources,
            EndpointType = endpointType,
            MessageTypes = 
                messageTypes
                ?? endpointType?.GetHandlerTypes()
                ?? [],
        };
    }
}