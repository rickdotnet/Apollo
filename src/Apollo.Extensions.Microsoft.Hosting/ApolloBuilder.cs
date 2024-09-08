using Apollo.Abstractions;
using Apollo.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Apollo.Extensions.Microsoft.Hosting;

public interface IApolloBuilder
{
    public IServiceCollection Services { get; }
    IApolloBuilder AddEndpoint<TEndpoint>(EndpointConfig config) where TEndpoint : class;
    IApolloBuilder AddHandler(EndpointConfig config, Func<ApolloContext, CancellationToken, Task> handler);
    IApolloBuilder WithEndpointProvider(IEndpointProvider endpointProvider);
}

public class ApolloBuilder : IApolloBuilder
{
    public IServiceCollection Services { get; }
    private IEndpointProvider? defaultEndpointProvider;

    public ApolloBuilder(IServiceCollection services)
    {
        Services = services;
    }

    public IApolloBuilder AddEndpoint<TEndpoint>(EndpointConfig config) where TEndpoint : class
    {
        Services.TryAddSingleton<TEndpoint>();
        Services.AddSingleton<IEndpointRegistration>(EndpointRegistration.From<TEndpoint>(config));

        return this;
    }

    public IApolloBuilder AddHandler(EndpointConfig config, Func<ApolloContext, CancellationToken, Task> handler)
    {
        Services.AddSingleton<IEndpointRegistration>(EndpointRegistration.From(config, handler));
        return this;
    }

    public IApolloBuilder WithEndpointProvider(IEndpointProvider endpointProvider)
    {
        defaultEndpointProvider = endpointProvider;
        return this;
    }
    
    public void Build()
    {
        Services.AddSingleton<ApolloClient>();
        Services.AddHostedService<ApolloBackgroundService>();

        if (defaultEndpointProvider is not null)
            Services.AddSingleton<IEndpointProvider>(defaultEndpointProvider);
        else
            Services.TryAddSingleton<IEndpointProvider, DefaultEndpointProvider>();
    }
}