using NATS.Client.Core;

namespace Apollo.Core.Nats;

public class NatsSubscriptionConfig
{
    public string Namespace { get; set; }
    public Type EndpointType { get; init; }
    public string EndpointName { get; init; }
    public Dictionary<string, Type> MessageTypes { get; init; }
    public string EndpointSubject { get; init; }
    public string ConsumerName { get; set;}
    public NatsSubOpts? NatsSubOpts { get; set;}
    
    public INatsDeserialize<byte[]>? Serializer { get; set;}
}

    
