## Getting Started

Install Apollo via NuGet:

```sh
dotnet add package RickDotNet.Apollo
```

### Quickstart

The following example demonstrates how to set up Apollo with an In-Memory provider. This basic example will help you understand the core concepts and get up and running quickly.

```csharp
var apolloConfig = new ApolloConfig { InstanceId = "my-instance", DefaultConsumerName = "my-consumer" };
var client = new ApolloClient(apolloConfig, endpointProvider: new EndpointProvider());

// configure and add an endpoint
var endpointConfig = new EndpointConfig
{
    Namespace = "dev.myapp",
    EndpointName = "My Endpoint"
};

var endpoint = client.AddEndpoint<TestEndpoint>(endpointConfig);
_ = endpoint.StartEndpoint(CancellationToken.None);

// Clean up
await endpoint.DisposeAsync();
```

### Handling Messages

**Endpoint**
```csharp
public record MyMessage(string Message) : IEvent;

public class TestEndpoint : IListenFor<MyMessage>
{
    public Task HandleAsync(MyMessage message, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"Endpoint Received: {message.Message}");
        return Task.CompletedTask;
    }
}
```

**Handler**
```csharp
var anonEndpoint = client.AddHandler(endpointConfig, OnMessageReceived);
_ = anonEndpoint.StartEndpoint(CancellationToken.None);

Task OnMessageReceived(ApolloContext context, CancellationToken cancellationToken)
{
    Console.WriteLine($"Received message");
    return Task.CompletedTask;
}

```

**Publishing**
```
var publisher = client.CreatePublisher(endpointConfig);

await Task.WhenAll(
    publisher.BroadcastAsync(new MyMessage("message 1"), CancellationToken.None),
    publisher.BroadcastAsync(new MyMessage("message 2"), CancellationToken.None),
    publisher.BroadcastAsync(new MyMessage("message 3"), CancellationToken.None)
);

Console.WriteLine("Messages published!");
Console.ReadKey();
```

### In-Memory Demo

```cs
using Apollo;
using Apollo.Abstractions;
using Apollo.Configuration;

ar apolloConfig = new ApolloConfig { InstanceId = "my-instance", DefaultConsumerName = "my-consumer" };
var client = new ApolloClient(apolloConfig, endpointProvider: new EndpointProvider());

// Configure and add an endpoint
var endpointConfig = new EndpointConfig
{
    Namespace = "dev.myapp",
    EndpointName = "My Endpoint"
};

var anonEndpoint = client.AddHandler(endpointConfig, OnMessageReceived);
var endpoint = client.AddEndpoint<TestEndpoint>(endpointConfig);

_ = anonEndpoint.StartEndpoint(CancellationToken.None);
_ = endpoint.StartEndpoint(CancellationToken.None);

// Create a publisher
var publisher = client.CreatePublisher(endpointConfig);

// Publish some messages
await Task.WhenAll(
    publisher.BroadcastAsync(new MyMessage("message 1"), CancellationToken.None),
    publisher.BroadcastAsync(new MyMessage("message 2"), CancellationToken.None),
    publisher.BroadcastAsync(new MyMessage("message 3"), CancellationToken.None)
);

Console.WriteLine("Messages published!");
Console.WriteLine("Press any key to exit.");
Console.ReadKey();

// Clean up
await anonEndpoint.DisposeAsync();

return;

Task OnMessageReceived(ApolloContext context, CancellationToken cancellationToken)
{
    Console.WriteLine($"Received message");
    return Task.CompletedTask;
}

public record MyMessage(string Message) : IEvent;

public class TestEndpoint : IListenFor<MyMessage>
{
    public Task HandleAsync(MyMessage message, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"Endpoint Received: {message.Message}");
        return Task.CompletedTask;
    }
}

public class EndpointProvider : IEndpointProvider
{
    public object? GetService(Type endpointType)
    {
        return new TestEndpoint();
    }
}
```


