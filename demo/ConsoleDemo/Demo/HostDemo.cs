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
        #region docs-snippet-host

        var anonConfig = new EndpointConfig { ConsumerName = "anon", Subject = "demo" };

        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddApollo(
            ab =>
            {
                ab
                    .WithConfig(new ApolloConfig())
                    .WithDefaultConsumerName("default-consumer")
                    .AddEndpoint<TestEndpoint>(TestEndpoint.EndpointConfig)
                    .AddHandler(anonConfig, (context, _) =>
                    {
                        var message = context.Data!.As<TestEvent>();
                        Console.WriteLine($"AnonHandler: {message?.Message}");

                        return Task.CompletedTask;
                    });

                if (useNats)
                {
                    ab.AddNatsProvider(
                        opts => opts with
                        {
                            Url = "nats://localhost:4222",
                            // AuthOpts = new NatsAuthOpts
                            // {
                            //     Username = "apollo",
                            //     Password = "demo"
                            // }
                        }
                    );
                }
            }
        );

        #endregion

        var host = builder.Build();
        var hostTask = host.RunAsync();

        await Task.Delay(3000);
        using var scope = host.Services.CreateScope();
        var serviceProvider = scope.ServiceProvider;

        #region docs-snippet-publish

        var apollo = serviceProvider.GetRequiredService<ApolloClient>();
        var publisher = apollo.CreatePublisher(TestEndpoint.EndpointConfig);

        await Task.WhenAll(
            publisher.Broadcast(new TestEvent("test 1"), CancellationToken.None),
            publisher.Broadcast(new TestEvent("test 2"), CancellationToken.None),
            publisher.Broadcast(new TestEvent("test 3"), CancellationToken.None),
            publisher.Broadcast(new TestEvent("test 4"), CancellationToken.None),
            publisher.Broadcast(new TestEvent("test 5"), CancellationToken.None)
        );

        #endregion

        Console.WriteLine("Press any key to exit");
        Console.ReadKey();
    }
}