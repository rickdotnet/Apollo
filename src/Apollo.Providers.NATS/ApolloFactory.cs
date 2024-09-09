using Apollo.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NATS.Client.Core;

namespace Apollo.Providers.NATS;

public class ApolloFactory
{
    private readonly INatsConnection connection;
    private readonly ILoggerFactory loggerFactory;

    public ApolloFactory(INatsConnection natsConnection, ILoggerFactory? loggerFactory)
    {
        connection = natsConnection;
        this.loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
    }
    
    public ISubscriptionProvider CreateSubscriptionProvider() 
        => new NatsSubscriptionProvider(connection, loggerFactory );

    public IProviderPublisher CreatePublisher()
        => new NatsPublisher(connection);
}