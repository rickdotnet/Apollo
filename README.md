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

The API is very much in design right now. This is the current POC usage.

```cs
var config = new ApolloConfig("nats://localhost:4222");

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddApollo(config)
    .WithEndpoints(
        endpoints =>
        {
            endpoints.AddEndpoint<MyEndpoint>(
              cfg =>
                {
                  cfg.IsLocalEndpoint = true;
                  cfg.ConsumerName = "DifferentConsumerName";
                });
            endpoints.AddEndpoint<MyOtherEndpoint>(
              cfg =>
                {
                  cfg.IsLocalEndpoint = true;
                  cfg.ConsumerName = "MyOtherEndpoint";
                }
              );
        });

var host = builder.Build();

var localDispatcher = host.Services.GetRequiredService<ILocalPublisher>();
//var remoteDispatcher = host.Services.GetRequiredService<IRemotePublisher>();

await localDispatcher.BroadcastAsync(new TestMessage("Local Test Event"));
//await remoteDispatcher.SendCommandAsync(new TestCommand("Remote Test Command"));

await host.RunAsync();
```

```cs
public class MyEndpoint : IListenFor<TestMessage>, IHandle<TestCommand>
{
    public ValueTask HandleEventAsync(TestMessage message, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"MyEndpoint Received: {message.Message}");
        return ValueTask.CompletedTask;
    }


    public ValueTask HandleCommandAsync(TestCommand message, CancellationToken cancellationToken)
    {
        Console.WriteLine($"MyEndpoint Received: {message.Message}");
        return ValueTask.CompletedTask;
    }
}
```
