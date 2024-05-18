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
        Action<NatsOpts>? natsOptsAction = null)
    {
        apolloBuilder.Services.TryAddSingleton<NatsSubscriber>();
        apolloBuilder.Services.AddSingleton<ISubscriber>(x => x.GetRequiredService<NatsSubscriber>());
        
        apolloBuilder.Services.AddSingleton<NatsRemotePublisherFactory>();
        apolloBuilder.Services.AddSingleton<INatsRemotePublisherFactory>(x => x.GetRequiredService<NatsRemotePublisherFactory>());
        apolloBuilder.Services.AddSingleton<IRemotePublisherFactory>(x => x.GetRequiredService<NatsRemotePublisherFactory>());

        apolloBuilder.Services.AddNatsClient(
            nats => nats.ConfigureOptions(ops => ops with
            {
                Url = apolloBuilder.Config.Url,
                ConnectTimeout = TimeSpan.FromSeconds(10),
                RequestTimeout = TimeSpan.FromSeconds(10),
                AuthOpts = NatsAuthOpts.Default with
                {
                    CredsFile = apolloBuilder.Config.CredsFile,
                    Token = apolloBuilder.Config.Token,
                    NKey = apolloBuilder.Config.NKey,
                    Seed = apolloBuilder.Config.Seed,
                    Jwt = apolloBuilder.Config.Jwt,
                }
            }));

        
        return apolloBuilder;
    }
}