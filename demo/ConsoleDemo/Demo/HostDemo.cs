using Apollo;
using Apollo.Configuration;
using Apollo.Extensions.Microsoft.Hosting;
using Apollo.Providers.NATS;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NATS.Client.Core;

namespace ConsoleDemo.Demo;

public static class HostDemo
{
    public static async Task Demo(bool useNats = false)
    {
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
    }
}