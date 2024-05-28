using Apollo.Messaging.Abstractions;
using Apollo.Messaging.Endpoints;

namespace BlazorDemo.Endpoints;

public record TestEvent(string Message) : IEvent;

// EndpointBase is optional, but provides access to the MesssageContext
public class TestEndpoint : EndpointBase, IListenFor<TestEvent>
{
    private readonly ILogger<TestEndpoint> logger;

    public TestEndpoint(ILogger<TestEndpoint> logger)
    {
        this.logger = logger;
    }
    public Task HandleAsync(TestEvent message, CancellationToken cancellationToken = default)
    {
        logger.LogTrace("TestEndpoint Received: {Message}", message.Message);
        return Task.FromResult(true);
    }
}