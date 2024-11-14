using Apollo.Abstractions;
using Apollo.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Apollo.Extensions.Microsoft.Hosting;

public interface IApolloBuilder
{
    public IServiceCollection Services { get; }
    IApolloBuilder WithConfig(ApolloConfig config);
    IApolloBuilder WithInstanceId(string instanceId);
    IApolloBuilder WithDefaultConsumerName(string consumerName);
    IApolloBuilder WithDefaultNamespace(string defaultNamespace);
    IApolloBuilder CreateMissingResources(bool createMissingResources = true);
    IApolloBuilder WithAckStrategy(AckStrategy ackStrategy);
    IApolloBuilder PublishOnly(bool publishOnly = true);
    IApolloBuilder AddEndpoint<TEndpoint>(EndpointConfig config) where TEndpoint : class;
    IApolloBuilder AddHandler(EndpointConfig config, Func<ApolloContext, CancellationToken, Task> handler);
    IApolloBuilder WithEndpointProvider(IEndpointProvider endpointProvider);
    IApolloBuilder WithEndpointProvider<TProvider>() where TProvider : class, IEndpointProvider;
    IApolloBuilder WithSubscriberProvider(ISubscriptionProvider subscriberProvider);
    IApolloBuilder WithSubscriberProvider<TProvider>() where TProvider : class, ISubscriptionProvider;
    IApolloBuilder WithProviderPublisher(IProviderPublisher providerPublisher);
    IApolloBuilder WithProviderPublisher<TPublisher>() where TPublisher : class, IProviderPublisher;
}

internal class ApolloBuilder : IApolloBuilder
{
    public IServiceCollection Services { get; }
    private ApolloConfig config = new();

    public ApolloBuilder(IServiceCollection services)
    {
        Services = services;
    }

    /// <summary>
    /// Set the Apollo configuration
    /// </summary>
    public IApolloBuilder WithConfig(ApolloConfig apolloConfig)
    {
        config = apolloConfig;
        return this;
    }

    /// <summary>
    /// Unique identifier for the process
    /// </summary>
    public IApolloBuilder WithInstanceId(string instanceId)
    {
        config.InstanceId = instanceId;
        return this;
    }

    /// <summary>
    /// Uniquely identifies the consumer of messages
    /// </summary>
    public IApolloBuilder WithDefaultConsumerName(string consumerName)
    {
        config.DefaultConsumerName = consumerName;
        return this;
    }

    /// <summary>
    /// Set the default namespace for endpoint and publisher messages
    /// </summary>
    public IApolloBuilder WithDefaultNamespace(string defaultNamespace)
    {
        config.DefaultNamespace = defaultNamespace;
        return this;
    }

    /// <summary>
    ///  Allow subscribers to create missing resources
    /// </summary>
    public IApolloBuilder CreateMissingResources(bool createMissingResources = true)
    {
        config.CreateMissingResources = createMissingResources;
        return this;
    }

    public IApolloBuilder WithAckStrategy(AckStrategy ackStrategy)
    {
        config.AckStrategy = ackStrategy;
        return this;
    }

    /// <summary>
    /// Set the service to publish only mode
    /// </summary>
    public IApolloBuilder PublishOnly(bool publishOnly = true)
    {
        config.PublishOnly = publishOnly;
        return this;
    }

    /// <summary>
    /// Add an endpoint to the service collection and register it with Apollo
    /// </summary>
    public IApolloBuilder AddEndpoint<TEndpoint>(EndpointConfig config) where TEndpoint : class
    {
        Services.TryAddSingleton<TEndpoint>();
        Services.AddSingleton<IEndpointRegistration>(EndpointRegistration.From<TEndpoint>(config));

        return this;
    }

    /// <summary>
    /// Add a handler registration to the service collection
    /// </summary>
    public IApolloBuilder AddHandler(EndpointConfig config, Func<ApolloContext, CancellationToken, Task> handler)
    {
        Services.AddSingleton<IEndpointRegistration>(EndpointRegistration.From(config, handler));
        return this;
    }

    /// <summary>
    /// Set the default endpoint provider implementation to use
    /// </summary>
    public IApolloBuilder WithEndpointProvider(IEndpointProvider endpointProvider)
    {
        Services.TryAddSingleton<IEndpointProvider>(endpointProvider);
        return this;
    }

    /// <summary>
    /// Add and use an endpoint provider from the service collection
    /// </summary>
    /// <typeparam name="TProvider"></typeparam>
    /// <returns></returns>
    public IApolloBuilder WithEndpointProvider<TProvider>() where TProvider : class, IEndpointProvider
    {
        Services.TryAddSingleton<TProvider>();
        Services.TryAddSingleton<IEndpointProvider>(serviceProvider => serviceProvider.GetRequiredService<TProvider>());
        return this;
    }

    public IApolloBuilder WithSubscriberProvider(ISubscriptionProvider subscriberProvider)
    {
        Services.TryAddSingleton<ISubscriptionProvider>(subscriberProvider);
        return this;
    }

    public IApolloBuilder WithSubscriberProvider<TProvider>() where TProvider : class, ISubscriptionProvider
    {
        Services.TryAddSingleton<TProvider>();
        Services.TryAddSingleton<ISubscriptionProvider>(serviceProvider => serviceProvider.GetRequiredService<TProvider>());
        return this;
    }

    public IApolloBuilder WithProviderPublisher(IProviderPublisher providerPublisher)
    {
        Services.TryAddSingleton<IProviderPublisher>(providerPublisher);
        return this;
    }

    public IApolloBuilder WithProviderPublisher<TPublisher>() where TPublisher : class, IProviderPublisher
    {
        Services.TryAddSingleton<TPublisher>();
        Services.TryAddSingleton<IProviderPublisher>(serviceProvider => serviceProvider.GetRequiredService<TPublisher>());
        return this;
    }

    internal void Build()
    {
        Services.AddSingleton(config);
        Services.AddSingleton<ApolloClient>();

        // If in publish only mode, do not register subscription services
        if (config.PublishOnly) return;

        Services.AddHostedService<ApolloBackgroundService>();

        // If no endpoint provider has been set, use the DefaultEndpointProvider
        Services.TryAddSingleton<IEndpointProvider, DefaultEndpointProvider>();
    }
}