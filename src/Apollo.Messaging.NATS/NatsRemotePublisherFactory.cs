using Apollo.Messaging.Abstractions;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;

namespace Apollo.Messaging.NATS;

public interface INatsRemotePublisherFactory : IRemotePublisherFactory;

public class NatsRemotePublisherFactory : INatsRemotePublisherFactory
{
    private readonly INatsConnection connection;
    private readonly ILogger logger;

    public NatsRemotePublisherFactory(INatsConnection connection, ILoggerFactory loggerFactory)
    {
        this.connection = connection;
        logger = loggerFactory.CreateLogger<NatsPublisher>();
    }
    public IRemotePublisher CreatePublisher(string route) 
        => new NatsPublisher(route, connection, logger);
}