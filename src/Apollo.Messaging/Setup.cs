using Apollo.Configuration;
using Apollo.Messaging.Endpoints;
using Apollo.Messaging.Middleware;
using Apollo.Messaging.Time;
using IdGen;
using IdGen.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Apollo.Messaging;

public static class Setup
{
    public static ApolloBuilder WithEndpoints(this ApolloBuilder apolloBuilder,
        Action<IEndpointBuilder>? builderAction = null)
    {
        var services = apolloBuilder.Services;
        var endpointBuilder = new EndpointBuilder(services, apolloBuilder.Config);

        services.AddScoped<MiddlewareExecutor>();
        services.AddScoped<IMessageMiddleware, LoggingMiddleware>();
        services.AddScoped<IMessageMiddleware, EndpointMiddleware>();
        services.AddSingleton<MessageProcessor>();
        services.AddHostedService<SubscriptionBackgroundService>();
        services.TryAddSingleton<IPublisherFactory, PublisherFactory>();
        
        builderAction?.Invoke(endpointBuilder);
        
        // if time sync is enabled, this will track an offset
        // based on delay between our source and us
        // otherwise, it's the default timer source
        var timeSource = new ApolloIdGenTimeSource();
        services.AddSingleton<ApolloIdGenTimeSource>();
        
        // used to generate message ids
        // TODO: generator id should be unique per client/generator, should design and configure this
        var randomInt = new Random().Next(1, 1000); // lazy duplicate Id prevention
        services.AddIdGen(randomInt, () => new IdGeneratorOptions(timeSource: timeSource));
        
        return apolloBuilder;
    }

    public static void WithTimeSynchronizer(this ApolloBuilder apolloBuilder, TimeSyncMode timeSyncMode = TimeSyncMode.Receive)
    {
        if (timeSyncMode == TimeSyncMode.Receive)
        {
            // hopefully syncs an offset for us
            apolloBuilder.Services.TryAddSingleton<TimeSynchronizer>();
        }
        else
        {
            // we don't support time-publishing, yet
        }
    }
}