using Apollo.Configuration;
using Apollo.Messaging.Abstractions;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.Logging;

namespace Apollo.Messaging.Azure;

public interface IAzureRemotePublisherFactory : IRemotePublisherFactory;

public class AzureRemotePublisherFactory : IAzureRemotePublisherFactory
{
    private readonly ApolloConfig config;
    private readonly ServiceBusClient client;
    private readonly ServiceBusAdministrationClient adminClient;
    private readonly ILogger logger;


    public AzureRemotePublisherFactory(ApolloConfig config,
        ServiceBusClient client,
        ServiceBusAdministrationClient adminClient,
        ILoggerFactory loggerFactory)
    {
        this.config = config;
        this.client = client;
        this.adminClient = adminClient;
        logger = loggerFactory.CreateLogger<AzurePublisher>();
    }

    public IRemotePublisher CreatePublisher(string route)
        => new AzurePublisher(route,client, adminClient, logger);
}