using Apollo.Configuration;
using Apollo.Messaging.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace Apollo.Messaging.Publishing;

public interface IPublisherFactory
{
    IPublisher CreatePublisher(string route, PublisherType publisherType = PublisherType.Remote);
}
public class PublisherFactory : IPublisherFactory
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
        return publisherType switch
        {
            PublisherType.Local => serviceProvider.GetRequiredService<ILocalPublisherFactory>().CreatePublisher(route),
            PublisherType.Remote => serviceProvider.GetRequiredService<IRemotePublisherFactory>().CreatePublisher(route),
            _ => throw new ArgumentOutOfRangeException(nameof(publisherType), publisherType, null)
        };
    }
}