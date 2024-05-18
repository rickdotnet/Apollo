using Apollo.Configuration;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;

namespace Apollo.Messaging.NATS;

public class NatsSubscriber : ISubscriber
{
    private readonly INatsConnection connection;
    private readonly ILogger logger;

    public NatsSubscriber(
        INatsConnection connection,
        ILogger<NatsSubscriber> logger
    )
    {
        this.connection = connection;
        this.logger = logger;
    }

    public Task SubscribeAsync(SubscriptionConfig config, Func<ApolloMessage, CancellationToken, Task> handler,
        CancellationToken cancellationToken)
    {
        return config.IsDurableConsumer
            ? new NatsJetStreamSubscriber(connection, logger).SubscribeAsync(config, handler, cancellationToken)
            : new NatsCoreSubscriber(connection, logger).SubscribeAsync(config, handler, cancellationToken);
    }
}