using Apollo.Configuration;

namespace Apollo.Abstractions;

public interface IProviderPublisher
{
    Task PublishAsync(PublishConfig publishConfig, ApolloMessage message, CancellationToken cancellationToken);
    
    Task<byte[]> RequestAsync(PublishConfig publishConfig, ApolloMessage message, CancellationToken cancellationToken);
}