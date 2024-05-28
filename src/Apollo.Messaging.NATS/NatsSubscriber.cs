using Apollo.Configuration;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;

namespace Apollo.Messaging.NATS;

public class NatsSubscriber : ISubscriber
{
    private readonly INatsConnection connection;
    private readonly ILoggerFactory loggerFactory;

    public NatsSubscriber(
        INatsConnection connection,
        ILoggerFactory loggerFactory
    )
    {
        this.connection = connection;
        this.loggerFactory = loggerFactory;
    }

    public Task SubscribeAsync(SubscriptionConfig config, Func<ApolloMessage, CancellationToken, Task> handler,
        CancellationToken cancellationToken)
    {
        return config.IsDurableConsumer
            ? new NatsJetStreamSubscriber(connection, loggerFactory.CreateLogger<NatsJetStreamSubscriber>()).SubscribeAsync(config, handler, cancellationToken)
            : new NatsCoreSubscriber(connection, loggerFactory.CreateLogger<NatsCoreSubscriber>()).SubscribeAsync(config, handler, cancellationToken);
    }
}