using Apollo.Configuration;
using Apollo.Messaging.Middleware;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Apollo.Messaging.Endpoints;

public interface IEndpointBuilder
{
    IEndpointRegistration AddEndpoint<T>();
    IEndpointRegistration AddEndpoint<T>(Action<EndpointConfig> action, string? apiRoute = null);
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

    public IEndpointRegistration AddEndpoint<T>()
        => AddEndpoint<T>(_ => { });

    public IEndpointRegistration AddEndpoint<T>(string apiRoute)
        => AddEndpoint<T>(_ => { }, apiRoute);

    public IEndpointRegistration AddEndpoint<T>(Action<EndpointConfig> action, string? apiRoute = null)
    {
        var endpointConfig = new EndpointConfig(config, apiRoute);
        action(endpointConfig);

        var registration = new EndpointRegistration<T>(endpointConfig, this);
        services.AddScoped(typeof(T));
        endpointRegistry.RegisterEndpoint(registration);

        return registration;
    }
    
    internal void AddService(Type type, ServiceLifetime lifetime = ServiceLifetime.Scoped)
    {
        services.Add(new ServiceDescriptor(type, type, lifetime));
    }
}

 static class EndpointRegistrationExtensions
{
    public static void WithWiretap<T>(this EndpointRegistration registration)
    {
        registration.AddWiretap<T>();
    }
}