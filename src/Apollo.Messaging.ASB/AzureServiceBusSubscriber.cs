using Apollo.Configuration;
using Apollo.Messaging.Abstractions;

namespace Apollo.Messaging.ASB;

public class AzureServiceBusSubscriber : ISubscriber
{
    public Task SubscribeAsync(SubscriptionConfig config, Func<ApolloMessage, CancellationToken, Task> handler, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}