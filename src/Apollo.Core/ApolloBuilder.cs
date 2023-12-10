using Apollo.Core.Configuration;
using Apollo.Core.Endpoints;
using Apollo.Core.Hosting;
using Apollo.Core.Messaging;
using Microsoft.Extensions.DependencyInjection;

namespace Apollo.Core;

public class ApolloBuilder(IServiceCollection services, ApolloConfig config)
{
    private readonly IServiceCollection services = services;
    private readonly ApolloConfig config = config;

    private readonly IEndpointBuilder endpointBuilder = new EndpointBuilder(services, config);

    public void WithEndpoints(Action<IEndpointBuilder> action)
    {
        action(endpointBuilder);
    }

    public void WithRemotePublishing()
    {
        services.AddSingleton<IRemotePublisherFactory, RemotePublisherFactory>();
    }
}

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
        services.AddSingleton<IApolloDispatcher, ApolloDispatcher>();
        services.AddSingleton<ILocalPublisher, LocalPublisher>();
        services.AddSingleton(endpointRegistry);
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
        //services.AddScoped<IEndpoint>(x=>x.GetRequiredService<T>());
    }
}