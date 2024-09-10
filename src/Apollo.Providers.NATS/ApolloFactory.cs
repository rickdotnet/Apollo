using Apollo.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NATS.Client.Core;

namespace Apollo.Providers.NATS;

/// <summary>
/// Provides a factory for creating Apollo components that use NATS as the underlying messaging system.
/// </summary>
/// <remarks>This factory does not manage the lifecycle of the providers it creates.</remarks>
public sealed class ApolloFactory
{
    private readonly INatsConnection connection;
    private readonly ILoggerFactory loggerFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApolloFactory"/> class.
    /// </summary>
    /// <param name="natsConnection">Underlying NATS connection</param>
    /// <param name="loggerFactory">(Optional) Logger Factory</param>
    public ApolloFactory(INatsConnection natsConnection, ILoggerFactory? loggerFactory)
    {
        connection = natsConnection;
        this.loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
    }

    public ISubscriptionProvider CreateSubscriptionProvider()
        => new NatsSubscriptionProvider(connection, loggerFactory);

    public IProviderPublisher CreatePublisher()
        => new NatsPublisher(connection);
}