using Apollo.Abstractions;
using Apollo.Configuration;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;

namespace Apollo.Providers.NATS;

internal class NatsSubscriptionProvider : ISubscriptionProvider
{
    private readonly NatsConnection connection;
    private readonly ILoggerFactory loggerFactory;

    public NatsSubscriptionProvider(NatsConnection connection, ILoggerFactory loggerFactory)
    {
        this.connection = connection;
        this.loggerFactory = loggerFactory;
    }

    public ISubscription AddSubscription(SubscriptionConfig config,
        Func<ApolloContext, CancellationToken, Task> handler)
    {
        if (config.IsDurable)
        {
            return new NatsJetStreamSubscription(
                connection,
                loggerFactory.CreateLogger<NatsJetStreamSubscription>(),
                config,
                handler);
        }
        else
        {
            return new NatsCoreSubscription(
                connection,
                loggerFactory.CreateLogger<NatsCoreSubscription>(),
                config,
                handler);
        }
    }
}