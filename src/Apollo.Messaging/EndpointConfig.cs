namespace Apollo.Configuration;

public class EndpointConfig
{
    public static EndpointConfig Default
        => new(ApolloConfig.Default);

    public bool IsLocalEndpoint { get; set; }
    public bool IsRemoteEndpoint => !IsLocalEndpoint;
    public string ConsumerName { get; set; }
    public DurableConfig DurableConfig { get; set; } = DurableConfig.Default;
    public string Namespace { get; set; }
    public bool UseEndpointNameInRoute { get; set; } = true;

    public EndpointConfig(ApolloConfig apolloConfig)
    {
        ConsumerName = apolloConfig.ConsumerName;
        Namespace = apolloConfig.DefaultNamespace;
    }
}