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
        logger.LogInformation("TestEndpoint Received: {Message}", message.Message);
        logger.LogInformation("Subject: {Subject}", Context.Subject);
        logger.LogInformation("Source: {Source}", Context.Source);
        logger.LogInformation("ReplyTo: {ReplyTo}", Context.ReplyTo);
        Context.Headers.ToList()
            .ForEach(x => logger.LogInformation("Header: {Key}={Value}", x.Key, x.Value));

        return Task.FromResult(true);
    }
}