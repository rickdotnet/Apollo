using Apollo.Abstractions.Messaging.Events;
using Microsoft.Extensions.Logging;

namespace TestHost;

public record TestMessage(string Message) : IEvent;

public class MyOtherEndpoint(ILogger<MyOtherEndpoint> logger) : IListenFor<TestMessage>
{
    public ValueTask HandleEventAsync(TestMessage message, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("MyOtherEndpoint: {Message}", message.Message);
        return ValueTask.CompletedTask;
    }
}