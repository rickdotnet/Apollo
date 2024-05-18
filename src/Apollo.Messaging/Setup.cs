using Apollo.Messaging.Abstractions;
using Apollo.Messaging.Endpoints;
using Apollo.Messaging.Middleware;
using Apollo.Messaging.Publishing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Apollo.Messaging;

public static class Setup
{
    private static bool skip = false;
    public static ApolloBuilder WithEndpoints(this ApolloBuilder apolloBuilder,
        Action<IEndpointBuilder>? builderAction = null)
    {
        var services = apolloBuilder.Services;
        var endpointBuilder = new EndpointBuilder(services, apolloBuilder.Config);
        builderAction?.Invoke(endpointBuilder);
        
        // look, we know what this is
        // it's a hack, but it's a worthy one
        // I don't want to move the middleware registration
        // just yet. This helps avoid multiple registry combinations
        // registering middleware multiple times
        if (!skip)
        {
            services.AddScoped<IMessageMiddleware, LoggingMiddleware>();
            services.AddScoped<IMessageMiddleware, EndpointMiddleware>();
            services.AddHostedService<SubscriptionBackgroundService>();
            skip = true;
        }
        services.TryAddScoped<MiddlewareExecutor>();
        services.TryAddSingleton<MessageProcessor>();
        
        apolloBuilder.Services.TryAddSingleton<IPublisherFactory, PublisherFactory>();
        apolloBuilder.Services.TryAddSingleton<ILocalPublisherFactory, LocalPublisherFactory>();

        // add the registries by subscriber type
        endpointBuilder.Build();
        return apolloBuilder;
    }
    
    public static ApolloBuilder PublishOnly(this ApolloBuilder apolloBuilder)
    {
        apolloBuilder.Services.TryAddSingleton<IPublisherFactory, PublisherFactory>();
        apolloBuilder.Services.TryAddSingleton<ILocalPublisherFactory, LocalPublisherFactory>();
        return apolloBuilder;
    }
}