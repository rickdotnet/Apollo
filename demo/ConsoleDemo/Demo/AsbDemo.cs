using System.Text;
using Apollo;
using Apollo.Configuration;
using Apollo.Extensions.Microsoft.Hosting;
using Apollo.Providers.ASB;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace ConsoleDemo.Demo;

public class AsbDemo
{
    public static async ValueTask Demo()
    {
        var anonConfig = new EndpointConfig
        {
            Subject = "topic-test", // topic name
            ConsumerName = "Sandbox.Test", // subscription name
            EndpointName = "Topic Test", // display only when subject is sent
        };
        
        var asbConfig = new AsbConfig
        {
            ConnectionString = "",
            // SubscriptionName = "",
            // CreateMissingResources = false
        };

        var builder = Host.CreateApplicationBuilder();
        builder.Services
            .AddApollo(apolloBuilder => apolloBuilder
                    .AddHandler(anonConfig, Handle)
                    .AddAsbProvider(asbConfig)
                );
        //.AddScoped<TestEndpoint>();

        var host = builder.Build();
        using var scope = host.Services.CreateScope();
        var serviceProvider = scope.ServiceProvider;
        var apollo = serviceProvider.GetRequiredService<ApolloClient>();

        var publisher = apollo.CreatePublisher(anonConfig);

        await Task.WhenAll(
            publisher.Broadcast(new TestEvent("test 1"), CancellationToken.None),
            publisher.Broadcast(new TestEvent("test 2"), CancellationToken.None),
            publisher.Broadcast(new TestEvent("test 3"), CancellationToken.None),
            publisher.Broadcast(new TestEvent("test 4"), CancellationToken.None),
            publisher.Broadcast(new TestEvent("test 5"), CancellationToken.None)
        );        

        Log.Verbose("Press any key to exit");
        Console.ReadKey();
    }

    static int count; // demo concurrency

    private static Task Handle(ApolloContext context, CancellationToken token)
    {
        Log.Warning($"Anonymous handler received: {count++}");
        Log.Debug("Headers");
        var msg = context.Message;
        foreach (var header in msg.Headers)
        {
            Log.Debug("{Key}: {Value}", header.Key, header.Value);
        }

        Log.Debug("Payload");
        if (msg.Data != null)
            Log.Debug(Encoding.UTF8.GetString(msg.Data));


        // let me see some messages before they spam through
        return Task.Delay(15000);
    }
}