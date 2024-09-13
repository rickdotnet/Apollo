using Apollo.Configuration;

namespace Apollo.Abstractions;

public interface IProviderPublisher
{
    Task Publish(PublishConfig publishConfig, ApolloMessage message, CancellationToken cancellationToken);
    
    Task<byte[]> Request(PublishConfig publishConfig, ApolloMessage message, CancellationToken cancellationToken);
}