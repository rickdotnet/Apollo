using Apollo.Abstractions;
using Serilog;

namespace ConsoleDemo;

public record TestEvent(string Message) : IEvent;
public record TestCommand(string Message) : ICommand;
public record TestRequest(string Message) : IRequest;
public record TestResponse(string Message);

public class TestEndpoint : IListenFor<TestEvent>, IHandle<TestCommand>, IReplyTo<TestRequest, TestResponse>
{
    private static int count = 0;
    public Task HandleAsync(TestEvent message, CancellationToken cancellationToken = default)
    {
        count++;
        Log.Information("Endpoint: {Message}, Count: {Count}", message, count);
        // simulate a delay to demonstrate concurrency
        return Task.Delay(500);
    }

    public Task HandleAsync(TestCommand message, CancellationToken cancellationToken)
    {
        Log.Information("TestEndpoint Received TestCommand");
        Log.Information("Message: {Message}", message);
        return Task.CompletedTask;
    }

    public Task<TestResponse> HandleAsync(TestRequest message, CancellationToken cancellationToken = default)
    { 
        Log.Information("TestEndpoint Received TestRequest");
        Log.Information("Message: {Message}", message);
        
        return Task.FromResult(new TestResponse("TestResponse"));
    }
}