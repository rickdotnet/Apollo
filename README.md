# Apollo

Apollo aims to be a lightweight, high-performance messaging client designed to provide developers with a simple yet powerful way to incorporate flexible architectures into their .NET applications. Initially built on top of the NATS messaging system, Apollo now also supports Azure Service Bus, offering even more flexibility for your messaging needs. With Apollo, you can easily handle events, commands, and requests with minimal configuration, while benefiting from the speed and scalability of NATS and the reliability of Azure Service Bus.

## RAPID DEVELOPMENT

Apollo is going under yet another rewrite. Practice makes perfect.

### Client Usage
```csharp
// var natsProvider = new NatsSubscriptionProvider();
var asbProvider = new AsbSubscriptionProvider();
var apollo = new ApolloClient(asbProvider);

var config = new EndpointConfig
{
    Namespace = "DEV", // optional prefix for isolation
    EndpointName = "My Endpoint", // slugified if no subject is provided
    EndpointSubject = "full-subject.control", // optional full control (see Namespace)
    ConsumerName = "unique-to-me", // required for load balancing and durable scenarios
    IsDurable = false // marker for subscription providers
};

// endpoint with IHandle<>, etc
var endpoint = apollo.AddEndpoint<TestEndpoint>(config);

// on the fly
var anonEndpoint = apollo.AddHandler(config, (context, token) =>
{
    // simulate checking token
    if(token.IsCancellationRequested)
        return Task.CompletedTask;
    
    // simulate message
    var message = context.Message;
    Console.WriteLine(message);

    return Task.CompletedTask;
});

await endpoint.DisposeAsync();
await anonEndpoint.DisposeAsync();
```

### Test Endpoint
```csharp
public record TestEvent(string Message) : IEvent;
public record TestCommand(string Message) : ICommand;

public class TestEndpoint : IListenFor<TestEvent>, IHandle<TestCommand>
{
    public Task HandleAsync(TestEvent message, CancellationToken cancellationToken = default)
    {
        Log.Information("TestEndpoint Received TestEvent");
        Log.Information("Message: {Message}", message);
        return Task.CompletedTask;
    }

    public Task HandleAsync(TestCommand message, CancellationToken cancellationToken)
    {
        Log.Information("TestEndpoint Received TestCommand");
        Log.Information("Message: {Message}", message);
        return Task.CompletedTask;
    }
}
```