using Apollo.Configuration;
using Apollo.Messaging.Middleware;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Apollo.Messaging.Endpoints;

public interface IEndpointBuilder
{
    IEndpointBuilder AddEndpoint<T>();
    IEndpointBuilder AddEndpoint<T>(string apiRoute);
    IEndpointBuilder AddEndpoint<T>(Action<EndpointConfig> action, string? apiRoute = null);
}

internal class EndpointBuilder : IEndpointBuilder
{
    private readonly IServiceCollection services;
    private readonly ApolloConfig config;
    private readonly EndpointRegistry endpointRegistry = new();

    public EndpointBuilder(IServiceCollection services, ApolloConfig config)
    {
        this.services = services;
        this.config = config;
        
        services.TryAddSingleton<IEndpointRegistry>(endpointRegistry);
    }

    public IEndpointBuilder AddEndpoint<T>()
        => AddEndpoint<T>(_ => { });
    
    public IEndpointBuilder AddEndpoint<T>(string apiRoute)
        => AddEndpoint<T>(_ => { }, apiRoute);

    public IEndpointBuilder AddEndpoint<T>(Action<EndpointConfig> action, string? apiRoute = null)
    {
        var endpointConfig = new EndpointConfig(config, apiRoute);
        action(endpointConfig);

        var registration = new EndpointRegistration<T>(endpointConfig);
        services.AddScoped(typeof(T));
        endpointRegistry.RegisterEndpoint(registration);

        return this;
    }
}