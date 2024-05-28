# Apollo

Apollo is a lightweight, high-performance messaging platform designed to provide developers with a simple yet powerful way to incorporate flexible architectures into their .NET applications. Initially built on top of the NATS messaging system, Apollo now also supports Azure Service Bus, offering even more flexibility for your messaging needs. With Apollo, you can easily handle events, commands, and requests with minimal configuration, while benefiting from the speed and scalability of NATS and the reliability of Azure Service Bus.

## Planned Features

- **Simple Configuration**: Set up your messaging handlers and publishers with just a few lines of code.
- **Auto-wiring of Consumers**: Apollo automatically discovers and wires up your message handlers, making it easy to scale your system.
- **Integration with .NET Core**: Designed to work seamlessly with the .NET Core dependency injection system.
- **High Throughput**: Leverages the performance of NATS and Azure Service Bus to handle high volumes of messages efficiently.
- **Flexible Serialization**: Supports various serialization formats to suit your needs.
- **Scalability**: Scales horizontally as your application grows, thanks to the underlying NATS and Azure Service Bus infrastructures.
- **Resilience**: Built-in fault tolerance and message retry mechanisms to ensure reliable message delivery.

## Getting Started

To get started with Apollo, you'll need to have a running instance of NATS or Azure Service Bus. You can find instructions on setting up NATS [here](https://docs.nats.io/running-a-nats-service/introduction) and Azure Service Bus [here]https://learn.microsoft.com/en-us/azure/service-bus-messaging/service-bus-quickstart-topics-subscriptions-portal).

Azure Service Bus Support
Apollo now supports Azure Service Bus (ASB). Note that ASB requires topics/subscriptions, so a Standard or Premium tier is necessary.

## Current Usage

The API is in rapid design right now. This is the current usage.

### Endpoint Host

```csharp
var builder = Host.CreateApplicationBuilder(args);
var config = new ApolloConfig() { CreateMissingResources = true };
builder.Services
    .AddApollo(
        config,
        apollo =>
        {
            apollo
                .UseNats()
                .UseAzure()
                .WithEndpoints(
                    endpoints =>
                    {
                        endpoints
                            //.AddEndpoint<MyEndpoint>()
                            .AddEndpoint<MyEndpoint>(cfg => cfg.SetDurableConsumer())
                            .AddEndpoint<MyOtherEndpoint>(cfg => cfg.SetLocalOnly())
                            .AddEndpoint<MyReplyEndpoint>()
                            .AddSubscriber<AzureServiceBusSubscriber>()
                            .AddSubscriber<NatsSubscriber>();
                    });
        });

var host = builder.Build();

await host.RunAsync();
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
    .AddApollo(config, x => x.WithEndpoints());

var host = builder.Build();
var publisherFactory = host.Services.GetRequiredService<IPublisherFactory>();

var remoteDispatcher = publisherFactory.CreatePublisher("TestEndpoint");
await remoteDispatcher.BroadcastAsync(new TestEvent("Hello, World!"), default);

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

Apollo aims to provide a robust and flexible messaging platform that can adapt to various messaging systems while maintaining a simple and intuitive API. Whether you prefer NATS for its speed and simplicity or Azure Service Bus for its enterprise-grade capabilities, Apollo has you covered.