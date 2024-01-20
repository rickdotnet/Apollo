using Apollo.Abstractions.Messaging.Commands;
using Apollo.Abstractions.Messaging.Events;
using Microsoft.Extensions.Logging;

namespace TestHost;

public record TestCommand : ICommand
{
    public TestCommand(string Message)
    {
        this.Message = Message;
    }

    public string Message { get; init; }

    public void Deconstruct(out string Message)
    {
        Message = this.Message;
    }
}

public class MyEndpoint : IListenFor<TestMessage>, IHandle<TestCommand>
{
    private readonly ILogger<MyEndpoint> logger1;

    public MyEndpoint(ILogger<MyEndpoint> logger)
    {
        logger1 = logger;
    }

    public ValueTask HandleEventAsync(TestMessage message, CancellationToken cancellationToken = default)
    {
        logger1.LogInformation("MyEndpoint Received: {Message}", message.Message);
        return ValueTask.CompletedTask;
    }

    public ValueTask HandleCommandAsync(TestCommand message, CancellationToken cancellationToken)
    {
        logger1.LogInformation("MyEndpoint Received: {Message}", message.Message);
        return ValueTask.CompletedTask;
    }
}