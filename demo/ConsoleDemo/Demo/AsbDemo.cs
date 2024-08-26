using System.Text;
using System.Text.Json;
using Apollo;
using Apollo.Configuration;
using Apollo.Providers.ASB;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace ConsoleDemo.Demo;

public class AsbDemo
{
    public static ValueTask Demo()
    {
        // will get fancy later
        var apolloConfig = new ApolloConfig
        {
            ProviderUrl = "", // asb connection string
            DefaultConsumerName = "", // default subscription name
            CreateMissingResources = false
        };

        var asbConfig = new AsbConfig
        {
            ConnectionString = apolloConfig.ProviderUrl,
            // SubscriptionName = "",
            // CreateMissingResources = false
        };

        var builder = Host.CreateApplicationBuilder();
        builder.Services
            .AddApollo(apolloConfig)
            .AddAsbProvider(asbConfig);
        //.AddScoped<TestEndpoint>();

        var host = builder.Build();
        using var scope = host.Services.CreateScope();
        var serviceProvider = scope.ServiceProvider;
        var apollo = serviceProvider.GetRequiredService<ApolloClient>();

        // public required string TopicName { get; init; }
        // public required string SubscriptionName { get; init; }
        // public bool IsDurable => true;
        // public bool CreateMissingResources { get; set; }
        var anonConfig = new EndpointConfig
        {
            EndpointSubject = "topic-test", // topic name
            ConsumerName = "Sandbox.Test", // subscription name
            EndpointName = "Topic Test", // display only when subject is sent
        };

        var anonEndpoint = apollo.AddHandler(anonConfig, Handle);

        _ = anonEndpoint.StartEndpoint(CancellationToken.None);

        Log.Verbose("Press any key to exit");
        Console.ReadKey();

        return anonEndpoint.DisposeAsync();
    }

    static int count = 0; // demo concurrency

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