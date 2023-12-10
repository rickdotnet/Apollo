namespace Apollo.Core.Configuration;

public class EndpointConfig(ApolloConfig apolloConfig)
{
    public bool IsLocalEndpoint { get; set; }
    public bool IsRemoteEndpoint => !IsLocalEndpoint;
    public string ConsumerName { get; set; } = apolloConfig.ConsumerName;
    public DurableConfig DurableConfig { get; set; } = DurableConfig.Default;
    public string Namespace { get; set; } = apolloConfig.DefaultNamespace;
    public bool UseEndpointNameInRoute { get; set; } = true;
    
    public static EndpointConfig Default 
        => new(ApolloConfig.Default);
}