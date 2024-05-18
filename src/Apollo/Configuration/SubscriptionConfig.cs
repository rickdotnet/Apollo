﻿namespace Apollo.Configuration;

public class SubscriptionConfig
{
    public required string Namespace { get; set; }
    public required Type EndpointType { get; init; }
    public required string EndpointName { get; init; }
    public required Dictionary<string, Type> MessageTypes { get; init; }
    public required string EndpointSubject { get; init; }
    public required string ConsumerName { get; set;}
    public required bool IsDurableConsumer { get; set;}
    public ISerializeThings? Serializer { get; set;}
}

    
