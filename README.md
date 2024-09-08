# Apollo

Apollo aims to be a lightweight, high-performance messaging client designed to provide developers with a simple yet powerful way to incorporate flexible architectures into their .NET applications. Initially built on top of the NATS messaging system, Apollo now also supports Azure Service Bus, offering even more flexibility for your messaging needs. With Apollo, you can easily handle events, commands, and requests with minimal configuration, while benefiting from the speed and scalability of NATS and the reliability of Azure Service Bus.

## RAPID DEVELOPMENT

Apollo is under rapid development. Feel free to pitch in! Join the fun at [discord.gg/rick](https://discord.gg/rick).

## Vision

The goal for this library is to provide a solid foundation, of which to build upon. The library should be easy to use, and provide a simple API for developers to work with. The library should be flexible, and allow developers to swap out the underlying messaging system with minimal effort.

And that's it. Keep it simple, and keep it flexible.

## Getting Started

To get started with Apollo you can run the code examples below or run the project in the `demo` folder. These will use the `InMemoryProvider` by default. In order to take full advantage of Apollo, you'll need to have a running instance of NATS or Azure Service Bus. You can find instructions on setting up NATS [here](https://docs.nats.io/running-a-nats-service/introduction) and Azure Service Bus [here](https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-quickstart-topics-subscriptions-portal).

### Azure Service Bus Support
Apollo now supports Azure Service Bus (ASB). Note that ASB requires topics/subscriptions, so a Standard or Premium tier is necessary.

## Code Example

### Test Endpoint

```csharp
public record TestEvent(string Message) : IEvent;

public class TestEndpoint : IListenFor<TestEvent>
{
    private static int count = 0;
    public Task HandleAsync(TestEvent message, CancellationToken cancellationToken = default)
    {
        count++; // thread safe when in syncmode
        Console.WriteLine($"Endpoint: {message}, Count: {count}");
        // simulate a delay to demonstrate concurrency
        return Task.Delay(500);
    }
}
```

### Dependency Injection Usage

The default implementation uses an InMemoryProvider to route messages to the appropriate endpoint. A poor man's Mediator.

```csharp
var endpointConfig = new EndpointConfig { ConsumerName = "endpoint", EndpointName = "Demo" };
var anonConfig = new EndpointConfig { ConsumerName = "anon", EndpointSubject = "demo.testevent" };

int count = 1; // thread-safe when in sync mode
var builder = Host.CreateApplicationBuilder();
builder.Services
    .AddApollo(
        apolloBuilder =>
        {
            apolloBuilder
                .AddEndpoint<TestEndpoint>(endpointConfig)
                .AddHandler(anonConfig, (context, token) =>
                {
                    Console.WriteLine($"Anonymous handler received: {count++}");
                    return Task.CompletedTask;
                });

            if (useNats)
            {
                apolloBuilder.AddNatsProvider(
                    opts => opts with
                    {
                        Url = "nats://localhost:4222",
                        AuthOpts = new NatsAuthOpts
                        {
                            Username = "apollo",
                            Password = "demo"
                        }
                    }
                );
            }
        }
    );

var host = builder.Build();
var hostTask = host.RunAsync();

await Task.Delay(8000);
using var scope = host.Services.CreateScope();
var serviceProvider = scope.ServiceProvider;
var apollo = serviceProvider.GetRequiredService<ApolloClient>();

var publisher = apollo.CreatePublisher(endpointConfig);

await Task.WhenAll(
    publisher.BroadcastAsync(new TestEvent("test 1"), CancellationToken.None),
    publisher.BroadcastAsync(new TestEvent("test 2"), CancellationToken.None),
    publisher.BroadcastAsync(new TestEvent("test 3"), CancellationToken.None),
    publisher.BroadcastAsync(new TestEvent("test 4"), CancellationToken.None),
    publisher.BroadcastAsync(new TestEvent("test 5"), CancellationToken.None)
);

Console.WriteLine("Press any key to exit");
Console.ReadKey();
```

### Direct Client Usage

Alternatively, you can use the ApolloClient directly, without the need for dependency injection.

```csharp
var endpointConfig = new EndpointConfig
{
    Namespace = "dev.myapp", // optional prefix for isolation
    EndpointName = "My Endpoint", // slugified if no subject is provided (my-endpoint)
    ConsumerName = "unique-to-me", // required for load balancing and durable scenarios
    IsDurable = false // marker for subscription providers
};

var endpointProvider = new EndpointProvider();
var demo = new ApolloClient(endpointProvider: endpointProvider);
var endpoint = demo.AddEndpoint<TestEndpoint>(endpointConfig);
_ = endpoint.StartEndpoint(CancellationToken.None);

var publisher = demo.CreatePublisher(endpointConfig);

await Task.WhenAll([
    publisher.BroadcastAsync(new TestEvent("test 1"), CancellationToken.None),
    publisher.BroadcastAsync(new TestEvent("test 2"), CancellationToken.None),
    publisher.BroadcastAsync(new TestEvent("test 3"), CancellationToken.None),
    publisher.BroadcastAsync(new TestEvent("test 4"), CancellationToken.None),
    publisher.BroadcastAsync(new TestEvent("test 5"), CancellationToken.None)
]);

Console.WriteLine("Press any key to exit");
Console.ReadKey();

// normally this is DI'd
class EndpointProvider : IEndpointProvider
{
    private readonly TestEndpoint testEndpoint = new();
    public object? GetService(Type endpointType) => testEndpoint;
}
```

## More to come

More documentation and examples are on the way. Stay tuned!