using Apollo.Configuration;

namespace Apollo.Messaging.Endpoints;

public class EndpointConfig
{
    public static EndpointConfig Default => new(ApolloConfig.Default);

    public bool LocalOnly { get; internal set; }
    public bool IsRemoteEndpoint => !LocalOnly;
    public string ConsumerName { get; init; }
    public DurableConfig DurableConfig { get; init; } = DurableConfig.Default;
    public string Namespace { get; init; }
    public bool UseEndpointNameInRoute { get; init; } = true;
    
    internal List<Type> LimitedSubscriberTypes { get; } = [];

    internal string? MinimalApiRoute { get; set; }

    public EndpointConfig(ApolloConfig apolloConfig, string? minimalApiRoute = null)
    {
        ConsumerName = apolloConfig.ConsumerName;
        Namespace = apolloConfig.DefaultNamespace;
    }
}

public static class EndpointConfigExtensions
{
    public static void SetDurableConsumer(this EndpointConfig config, bool durable = true)
    {
        config.DurableConfig.IsDurableConsumer = durable;
    }
    public static void SetLocalOnly(this EndpointConfig config, bool durable = true)
    {
        config.LocalOnly = durable;
    }
}