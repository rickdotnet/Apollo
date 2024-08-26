using Apollo;
using Apollo.Configuration;
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

        var builder = Host.CreateApplicationBuilder();
        builder.Services
            .AddApollo()
            .AddScoped<TestEndpoint>();

        if (useNats)
        {
            builder.Services
                .AddNatsProvider(opts => opts with
                {
                    Url = "nats://localhost:4222",
                    AuthOpts = new NatsAuthOpts
                    {
                        Username = "apollo",
                        Password = "demo"
                    }
                });
        }

        var host = builder.Build();
        using var scope = host.Services.CreateScope();
        var serviceProvider = scope.ServiceProvider;
        var apollo = serviceProvider.GetRequiredService<ApolloClient>();
        var endpoint = apollo.AddEndpoint<TestEndpoint>(endpointConfig);

        int count = 1; // thread-safe when in sync mode
        var anonEndpoint = apollo.AddHandler(anonConfig, (context, token) =>
        {
            Console.WriteLine($"Anonymous handler received: {count++}");
            return Task.CompletedTask;
        });

        _ = endpoint.StartEndpoint(CancellationToken.None);
        _ = anonEndpoint.StartEndpoint(CancellationToken.None);

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