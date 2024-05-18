using Apollo.Configuration;

namespace Apollo.Messaging.ASB;

public class AzureServiceBusSubscriber : ISubscriber
{
    public Task SubscribeAsync(SubscriptionConfig config, Func<ApolloMessage, CancellationToken, Task> handler, CancellationToken cancellationToken)
    {
        if (config.IsDurableConsumer)
        {
            // queue subscription
        }
        else
        {
            // topic
        }
        
        return Task.CompletedTask;
    }
}