using Apollo.Configuration;

namespace Apollo.Messaging.Endpoints;

public class EndpointConfig
{
    public static EndpointConfig Default
        => new(ApolloConfig.Default);

    public bool LocalOnly { get; init; }
    public bool IsRemoteEndpoint => !LocalOnly;
    public string ConsumerName { get; init; }
    public DurableConfig DurableConfig { get; init; } = DurableConfig.Default;
    public string Namespace { get; init; }
    public bool UseEndpointNameInRoute { get; init; } = true;

    public EndpointConfig(ApolloConfig apolloConfig)
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
}