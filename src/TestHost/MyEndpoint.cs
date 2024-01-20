using Apollo.Abstractions.Messaging.Commands;
using Apollo.Abstractions.Messaging.Events;
using Microsoft.Extensions.Logging;

namespace TestHost;

public record TestCommand(string Message) : ICommand;
public class MyEndpoint(ILogger<MyEndpoint> logger) : IListenFor<TestMessage>, IHandle<TestCommand>
{
    public ValueTask HandleEventAsync(TestMessage message, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("MyEndpoint Received: {Message}", message.Message);
        return ValueTask.CompletedTask;
    }

    public ValueTask HandleCommandAsync(TestCommand message, CancellationToken cancellationToken)
    {
        logger.LogInformation("MyEndpoint Received: {Message}", message.Message);
        return ValueTask.CompletedTask;
    }
}