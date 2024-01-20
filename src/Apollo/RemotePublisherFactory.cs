using Apollo.Configuration;
using Apollo.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;

namespace Apollo;

public interface IRemotePublisherFactory
{
    IRemotePublisher CreatePublisher(string endpointName);
}

internal class RemotePublisherFactory(IServiceProvider serviceProvider) : IRemotePublisherFactory
{
    private readonly IServiceProvider serviceProvider =
        serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

    private readonly ApolloConfig config = serviceProvider.GetRequiredService<ApolloConfig>();

    public IRemotePublisher CreatePublisher(string endpointName)
    {
        if (string.IsNullOrEmpty(endpointName))
            throw new ArgumentException("Endpoint name must not be null or empty.", nameof(endpointName));

        var connection = serviceProvider.GetRequiredService<INatsConnection>();
        var logger = serviceProvider.GetRequiredService<ILogger<RemotePublisher>>();

        return new RemotePublisher($"{config.DefaultNamespace}.{endpointName}", connection, logger);
    }
}