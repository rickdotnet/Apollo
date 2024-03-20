using Apollo.Configuration;
using Apollo.Messaging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;

namespace Apollo.Messaging;

public interface IPublisherFactory
{
    IPublisher CreatePublisher(string route, PublisherType publisherType = PublisherType.Remote);
    //IPublisher CreatePublisherInNamespace(string targetNamespace, string endpointName);
}

public class PublisherFactory: IPublisherFactory
{
    private readonly IServiceProvider serviceProvider;

    private readonly ApolloConfig config;

    public PublisherFactory(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        config = serviceProvider.GetRequiredService<ApolloConfig>();
    }
    
    public IPublisher CreatePublisher(string route, PublisherType publisherType = PublisherType.Remote)
    {
        ArgumentNullException.ThrowIfNull(route, nameof(route));
        ArgumentNullException.ThrowIfNull(publisherType, nameof(publisherType));

        route = $"{config.DefaultNamespace}.{route}";
        switch (publisherType)
        {
            case PublisherType.Local:
                return CreateLocalPublisher(route);
            case PublisherType.Remote:
                return CreateNatsPublisher(route);
        }
        throw new ArgumentOutOfRangeException(nameof(publisherType), publisherType, null);
    }
    private LocalPublisher CreateLocalPublisher(string route)
    {
        ArgumentNullException.ThrowIfNull(route, nameof(route));

        var messageProcessor = serviceProvider.GetRequiredService<MessageProcessor>();
        var logger = serviceProvider.GetRequiredService<ILogger<LocalPublisher>>();

        return new LocalPublisher(route, messageProcessor, logger);
    }
    private NatsPublisher CreateNatsPublisher(string route)
    {
        ArgumentNullException.ThrowIfNull(route, nameof(route));

        var connection = serviceProvider.GetRequiredService<INatsConnection>();
        var logger = serviceProvider.GetRequiredService<ILogger<NatsPublisher>>();

        return new NatsPublisher(route, connection, logger);
    }
}