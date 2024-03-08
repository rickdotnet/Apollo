using Apollo.Abstractions.Messaging.Commands;
using Apollo.Abstractions.Messaging.Events;
using Apollo.Messaging.Contracts;
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

public class MyEndpoint : IListenFor<TestEvent>, IHandle<TestCommand>
{
    private readonly ILogger<MyEndpoint> logger;

    public MyEndpoint(ILogger<MyEndpoint> logger)
    {
        this.logger = logger;
    }

    public Task HandleEventAsync(TestEvent message, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("MyEndpoint Received TestEvent: {Message}", message.Message);
        return Task.CompletedTask;
    }

    public Task HandleCommandAsync(TestCommand message, CancellationToken cancellationToken)
    {
        logger.LogInformation("MyEndpoint Received: {Message}", message.Message);
        return Task.CompletedTask;
    }
}