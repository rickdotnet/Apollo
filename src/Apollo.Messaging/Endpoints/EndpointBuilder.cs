using Apollo.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Apollo.Messaging.Endpoints;

public interface IEndpointBuilder
{
    IEndpointBuilder AddEndpoint<T>();
    IEndpointBuilder AddEndpoint<T>(string apiRoute);
    IEndpointBuilder AddEndpoint<T>(Action<EndpointConfig> action, string? apiRoute = null);
    IEndpointBuilder AddSubscriber<T>() where T : ISubscriber;
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
    }

    // allows AddSubscriber() to register multiple subscribers for the same registry
    public void Build() => services.AddSingleton<IEndpointRegistry>(endpointRegistry);

    public IEndpointBuilder AddEndpoint<T>()
        => AddEndpoint<T>(_ => { });

    public IEndpointBuilder AddEndpoint<T>(string apiRoute)
        => AddEndpoint<T>(_ => { }, apiRoute);

    public IEndpointBuilder AddEndpoint<T>(Action<EndpointConfig> action, string? apiRoute = null)
    {
        var endpointConfig = new EndpointConfig(config, apiRoute);
        action(endpointConfig);

        var registration = new EndpointRegistration<T>(endpointConfig);
        services.TryAddScoped(typeof(T));
        endpointRegistry.RegisterEndpoint(registration);

        return this;
    }

    public IEndpointBuilder AddSubscriber<T>() where T : ISubscriber
    {
        endpointRegistry.AddSubscriberType<T>();
        return this;
    }
}