using Apollo.Configuration;
using Apollo.Endpoints;
using Apollo.Hosting;
using Apollo.Messaging;
using Apollo.Messaging.Endpoints;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Apollo;

public interface IEndpointBuilder
{
    void AddEndpoint<T>();
    void AddEndpoint<T>(Action<EndpointConfig> action);
}

public class EndpointBuilder : IEndpointBuilder
{
    private readonly IServiceCollection services;
    private readonly ApolloConfig config;
    private IEndpointRegistry endpointRegistry = new EndpointRegistry();

    public EndpointBuilder(IServiceCollection services, ApolloConfig config)
    {
        this.services = services;
        this.config = config;
        services.TryAddScoped<IApolloDispatcher, ApolloDispatcher>();
        services.TryAddScoped<ILocalPublisher, LocalPublisher>();
        services.TryAddSingleton(endpointRegistry);
        services.AddHostedService<SubscriptionBackgroundService>();
        
        
    }

    public void AddEndpoint<T>()
        => AddEndpoint<T>(_ => { });

    public void AddEndpoint<T>(Action<EndpointConfig> action)
    {
        var endpointConfig = new EndpointConfig(config);
        action(endpointConfig);

        endpointRegistry.RegisterEndpoint(new EndpointRegistration<T>(endpointConfig));
        services.AddScoped(typeof(T));
    }
}