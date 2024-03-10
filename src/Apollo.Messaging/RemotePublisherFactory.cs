using Apollo.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;

namespace Apollo.Messaging;

public interface IRemotePublisherFactory
{
    IRemotePublisher CreatePublisher(string endpointName);
    IRemotePublisher CreatePublisherInNamespace(string targetNamespace, string endpointName);
}

internal class RemotePublisherFactory : IRemotePublisherFactory
{
    private readonly IServiceProvider serviceProvider;

    private readonly ApolloConfig config;

    public RemotePublisherFactory(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        config = serviceProvider.GetRequiredService<ApolloConfig>();
    }
    
    public IRemotePublisher CreatePublisher(string endpointName)
        => CreatePublisherInNamespace(config.DefaultNamespace, endpointName);

    public IRemotePublisher CreatePublisherInNamespace(string targetNamespace, string endpointName)
    {
        ArgumentNullException.ThrowIfNull(targetNamespace, nameof(targetNamespace));
        ArgumentNullException.ThrowIfNull(endpointName, nameof(endpointName));

        var connection = serviceProvider.GetRequiredService<INatsConnection>();
        var logger = serviceProvider.GetRequiredService<ILogger<RemotePublisher>>();

        return new RemotePublisher($"{targetNamespace}.{endpointName}", connection, logger);
    }
}