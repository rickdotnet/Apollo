using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.KeyValueStore;

namespace Apollo.Caching;

public static class Setup
{
    public static ApolloBuilder AddCaching(this ApolloBuilder builder)
    {
        builder.WithService(
            x => x.AddSingleton(provider =>
            {
                var js = new NatsJSContext(provider.GetRequiredService<NatsConnection>());
                var kv = new NatsKVContext(js);
                return new NatsDistributedCache(kv);
            }));

        builder.WithService(
            x =>
                x.AddSingleton<IDistributedCache>(provider => provider.GetRequiredService<NatsDistributedCache>()));
        
        return builder;
    }
}