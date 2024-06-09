using Apollo.Messaging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NATS.Client.Core;
using NATS.Extensions.Microsoft.DependencyInjection;

namespace Apollo.Messaging.NATS;

public static class Setup
{
    public static ApolloBuilder UseNats(
        this ApolloBuilder apolloBuilder,
        Func<NatsOpts, NatsOpts>? natsOptsFactory = null)
    {
        apolloBuilder.Services.TryAddSingleton<NatsSubscriber>();
        apolloBuilder.Services.AddSingleton<ISubscriber>(x => x.GetRequiredService<NatsSubscriber>());

        apolloBuilder.Services.AddSingleton<NatsRemotePublisherFactory>();
        apolloBuilder.Services.AddSingleton<INatsRemotePublisherFactory>(x =>
            x.GetRequiredService<NatsRemotePublisherFactory>());
        apolloBuilder.Services.AddSingleton<IRemotePublisherFactory>(x =>
            x.GetRequiredService<NatsRemotePublisherFactory>());

        // default factory does nothing
        natsOptsFactory ??= o => o;

        apolloBuilder.Services.AddNatsClient(
            nats => nats.ConfigureOptions(
                opts => natsOptsFactory(opts with { Url = apolloBuilder.Config.Url })
            )
        );

        return apolloBuilder;
    }
}