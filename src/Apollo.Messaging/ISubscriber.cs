using Apollo.Configuration;
using Apollo.Messaging.Abstractions;
using Apollo.Messaging.Replier;

namespace Apollo.Messaging;

public interface ISubscriber
{
    IReplier Replier => NoOpReplier.Instance;
    Task SubscribeAsync(SubscriptionConfig config, Func<ApolloMessage, CancellationToken, Task> handler,
        CancellationToken cancellationToken);
}