using Apollo.Configuration;

namespace Apollo.Extensions.Microsoft.Hosting;

public interface IEndpointRegistration
{
    EndpointConfig Config { get; }
    Type? EndpointType { get; }
    Func<ApolloContext, CancellationToken, Task>? Handler { get; }
    bool IsHandler => Handler is not null;
}

internal sealed class EndpointRegistration : IEndpointRegistration
{
    public EndpointConfig Config { get; }
    public Type? EndpointType { get; }
    
    public Func<ApolloContext, CancellationToken, Task>? Handler { get; }

    public EndpointRegistration(EndpointConfig config, Type endpointType)
    {
        Config = config;
        EndpointType = endpointType;
    }
    
    public EndpointRegistration(EndpointConfig config, Func<ApolloContext, CancellationToken, Task> handler)
    {
        Config = config;
        Handler = handler;
    }
    
    public static IEndpointRegistration From<T>(EndpointConfig config) 
        => new EndpointRegistration(config, typeof(T));
    
    public static IEndpointRegistration From(EndpointConfig config, Func<ApolloContext, CancellationToken, Task> handler)
    => new EndpointRegistration(config, handler);
}