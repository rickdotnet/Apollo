using Apollo;
using Apollo.Configuration;
using Apollo.Messaging;
using Apollo.Messaging.Abstractions;
using Apollo.Messaging.Azure;
using Apollo.Messaging.Contracts;
using Apollo.Messaging.NATS;
using Apollo.Messaging.Publishing;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace TestConsole;

public static class Scenarios
{
    // static ApolloConfig config = new()
    // {
    //     DefaultNamespace = "dev", 
    //     ConsumerName = "default-consumer"
    // };

    private static ApolloConfig config = ApolloConfig.Default;

    public static Task TestPublish()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Services
            .AddApollo(
                config,
                apollo =>
                {
                    apollo.UseNats();
                    //apollo.WithEndpoints();
                    apollo.PublishOnly();
                });

        var host = builder.Build();
        var publisherFactory = host.Services.GetRequiredService<IPublisherFactory>();

        var remotePublisher = publisherFactory.CreatePublisher("TestEndpoint");
        return remotePublisher.BroadcastAsync(new TestEvent("Hello World!"), default);

        //var response = await remoteDispatcher.SendRequestAsync<MyRequest,bool>(new MyRequest("TestRequest"), default);
        //Console.WriteLine($"Response: {response}");
    }

    public static async Task TestASB()
    {
        var connectionString = "<connection-string>";

        var client = new ServiceBusClient(connectionString);
        var adminClient = new ServiceBusAdministrationClient(connectionString);

        //var busManager = new BusResourceManager(client, adminClient);
        //await busManager.CreateReplyTopicAndSubscriptionAsync("apollo.requestline", "replies-dc1cfd6c-29bb-4967-95e9-f8186de9460b", default);

        var replyPublisher = new AzurePublisher("dev.myreplyendpoint", client, adminClient, NullLogger.Instance);
        var response = await replyPublisher.SendRequestAsync<MyRequest, bool>(new MyRequest("Test"), default);
        Console.WriteLine($"Response: {response}");

        var publisher = new AzurePublisher("dev.myendpoint", client, adminClient, NullLogger.Instance);
        //await publisher.BroadcastAsync(new TestEvent("Hello World!"), default);
        await publisher.SendCommandAsync(new TestCommand("Hello!"), default);
    }

    public record TestCommand(string Message) : ICommand;

    private record MyRequest(string Message) : IRequest<bool>;
}