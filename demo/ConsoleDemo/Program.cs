using Apollo;
using ConsoleDemo;
using Microsoft.Extensions.DependencyInjection;

var host = Demo.CreateHost(addProvider: false); // add NATS or na?
using var scope = host.Services.CreateScope();

var serviceProvider = scope.ServiceProvider;
var apollo = serviceProvider.GetRequiredService<ApolloClient>();

var endpoint = Demo.AddEndpoint<TestEndpoint>(apollo);
_ = endpoint.StartEndpoint(CancellationToken.None);

var anonEndpoint = Demo.AnonEndpoint(apollo);
_ = anonEndpoint.StartEndpoint(CancellationToken.None);

await Task.Delay(3000);

var publisher = apollo.CreatePublisher(Demo.PublishConfig);
await publisher.BroadcastAsync(new TestEvent("test message"), CancellationToken.None);
await publisher.BroadcastAsync(new TestEvent("test message"), CancellationToken.None);
await publisher.BroadcastAsync(new TestEvent("test message"), CancellationToken.None);
await publisher.BroadcastAsync(new TestEvent("test message"), CancellationToken.None);

Console.WriteLine("Press any key to exit");
Console.ReadKey();