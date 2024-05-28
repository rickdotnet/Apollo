using Apollo.Messaging.Abstractions;
using Apollo.Messaging.Contracts;
using Microsoft.Extensions.Logging;

namespace TestHost;

public class MyEndpoint : IListenFor<DemoMessages>, IHandle<TestCommand>
{
    private readonly ILogger<MyEndpoint> logger;

    public MyEndpoint(ILogger<MyEndpoint> logger) => this.logger = logger;

    public Task HandleAsync(DemoMessages message, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("MyEndpoint Received TestEvent: {Message}", message.Message);
        return Task.CompletedTask;
    }

    public Task HandleAsync(TestCommand message, CancellationToken cancellationToken)
    {
        logger.LogInformation("MyEndpoint Received: {Message}", message.Message);
        return Task.CompletedTask;
    }
}