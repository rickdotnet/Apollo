# Apollo

Apollo is a lightweight, high-performance messaging platform built on top of the NATS messaging system. It is designed to provide developers with a simple yet powerful way to incorporate flexible architectures into their .NET applications. With Apollo, you can easily handle events, commands, and requests with minimal configuration, while benefiting from the speed and scalability of NATS.

## Planned Features

- **Simple Configuration**: Set up your messaging handlers and publishers with just a few lines of code.
- **Auto-wiring of Consumers**: Apollo automatically discovers and wires up your message handlers, making it easy to scale your system.
- **Integration with .NET Core**: Designed to work seamlessly with the .NET Core dependency injection system.
- **High Throughput**: Leverages the performance of NATS to handle high volumes of messages efficiently.
- **Flexible Serialization**: Supports various serialization formats to suit your needs.
- **Scalability**: Scales horizontally as your application grows, thanks to the underlying NATS infrastructure.
- **Resilience**: Built-in fault tolerance and message retry mechanisms to ensure reliable message delivery.

## Getting Started

To get started with Apollo, you'll need to have a running instance of NATS. You can find instructions on setting up NATS [here](https://docs.nats.io/running-a-nats-service/introduction).

## Current Usage

The API is in rapid design right now. This is the current usage.

**Endpoint Host**
```cs
var builder = Host.CreateApplicationBuilder(args);

var config = ApolloConfig.Default;

builder.Services.AddApollo(
    config,
    apollo => apollo.WithEndpoints(endpoints => endpoints.AddEndpoint<TestEndpoint>()));

return builder.Build();
```

**Endpoint**
```cs
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
```

**External Publisher**
```cs
var config = ApolloConfig.Default;

var builder = Host.CreateApplicationBuilder(args);
builder.Services
    .AddApollo(config, x=>x.WithEndpoints());

var host = builder.Build();
var publisherFactory = host.Services.GetRequiredService<IPublisherFactory>();

var remoteDispatcher = publisherFactory.CreatePublisher("TestEndpoint");
await remoteDispatcher.BroadcastAsync(new TestEvent("Hello, World!"),default);

public record TestEvent(string Message) : IEvent;
```

**Local Publisher**
```cs
var localPublisher = publisherFactory.CreatePublisher(nameof(TestEndpoint), PublisherType.Local);
localPublisher.BroadcastAsync(new TestEvent("Hello, World!"), cancellationToken);
```

**NATS Cli**
```bash
nats pub apollo.default.testendpoint.testevent "{""message"":""Hello, World!""}"
```

**HTTP**
```
POST https://localhost:7199/endpoints/apollo.default.testendpoint.testevent
Accept: application/json
Content-Type: application/json

{
  "message": "Hello, World!"
}
```