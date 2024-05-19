namespace Apollo.Configuration;

public class SubscriptionConfig
{
    public Guid InstanceId { get; set; }
    public required string Namespace { get; set; }
    public required Type EndpointType { get; init; }
    public required string EndpointName { get; init; }
    public required Dictionary<string, Type> MessageTypes { get; init; }
    public required string EndpointSubject { get; set; }
    public required string ConsumerName { get; set;}
    public required bool IsDurableConsumer { get; set;}
    public bool CreateMissingResources { get; set; }
    public ISerializeThings? Serializer { get; set;}
}

    
