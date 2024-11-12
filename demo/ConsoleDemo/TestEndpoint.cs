using Apollo;
using Apollo.Abstractions;
using Apollo.Configuration;
using Serilog;

namespace ConsoleDemo;

public record TestEvent(string Message) : IEvent;
public record TestCommand(string Message) : ICommand;
public record TestRequest(string Message) : IRequest<TestResponse>;
public record TestResponse(string Message);

public class TestEndpoint : IListenFor<TestEvent>, IHandle<TestCommand>, IReplyTo<TestRequest, TestResponse>
{
    public static readonly EndpointConfig Default =  new EndpointConfig { ConsumerName = "endpoint", EndpointName = "Demo" };
    private static int count = 0;
    public Task Handle(TestEvent message, ApolloContext context, CancellationToken cancellationToken = default)
    {
        count++;
        Log.Information("Endpoint: {Message}, Count: {Count}", message, count);
        // simulate a delay to demonstrate concurrency
        return Task.Delay(500);
    }

    public Task Handle(TestCommand message, ApolloContext context, CancellationToken cancellationToken)
    {
        Log.Information("TestEndpoint Received TestCommand");
        Log.Information("Message: {Message}", message);
        return Task.CompletedTask;
    }

    public Task<TestResponse> Handle(TestRequest message, ApolloContext context, CancellationToken cancellationToken = default)
    { 
        Log.Information("TestEndpoint Received TestRequest");
        Log.Information("Message: {Message}", message);
        
        return Task.FromResult(new TestResponse("TestResponse"));
    }
}