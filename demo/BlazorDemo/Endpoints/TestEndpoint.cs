using Apollo.Messaging.Abstractions;

namespace BlazorDemo.Endpoints;

public record TestEvent(string Message) : IEvent;

public class TestEndpoint : IListenFor<TestEvent>
{
    public Task HandleAsync(TestEvent message, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"TestEndpoint Received TestEvent: {message.Message}");
        return Task.CompletedTask;
    }
}