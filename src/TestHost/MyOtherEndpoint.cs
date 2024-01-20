using Apollo.Abstractions.Messaging.Events;
using Microsoft.Extensions.Logging;

namespace TestHost;

public record TestMessage : IEvent
{
    public TestMessage(string Message)
    {
        this.Message = Message;
    }

    public string Message { get; init; }

    public void Deconstruct(out string Message)
    {
        Message = this.Message;
    }
}

public class MyOtherEndpoint : IListenFor<TestMessage>
{
    private readonly ILogger<MyOtherEndpoint> logger1;

    public MyOtherEndpoint(ILogger<MyOtherEndpoint> logger)
    {
        logger1 = logger;
    }

    public ValueTask HandleEventAsync(TestMessage message, CancellationToken cancellationToken = default)
    {
        logger1.LogInformation("MyOtherEndpoint: {Message}", message.Message);
        return ValueTask.CompletedTask;
    }
}