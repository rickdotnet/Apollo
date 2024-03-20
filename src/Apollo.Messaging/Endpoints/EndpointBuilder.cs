using Apollo.Configuration;
using Apollo.Messaging.Middleware;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Apollo.Messaging.Endpoints;

public interface IEndpointBuilder
{
    void AddEndpoint<T>();
    void AddEndpoint<T>(Action<EndpointConfig> action);
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
        services.AddScoped<MiddlewareExecutor>();
        services.AddScoped<IMessageMiddleware, LoggingMiddleware>();
        services.AddScoped<IMessageMiddleware, EndpointMiddleware>();
        services.AddSingleton<MessageProcessor>();
        services.AddHostedService<SubscriptionBackgroundService>();
    }

    public void AddEndpoint<T>()
        => AddEndpoint<T>(_ => { });

    public void AddEndpoint<T>(Action<EndpointConfig> action)
    {
        var endpointConfig = new EndpointConfig(config);
        action(endpointConfig);

        var registration = new EndpointRegistration<T>(endpointConfig);
        services.AddScoped(typeof(T));
        endpointRegistry.RegisterEndpoint(registration);
    }
}