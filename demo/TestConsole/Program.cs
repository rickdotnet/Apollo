using Apollo;
using Apollo.Configuration;
using Apollo.Messaging;
using Apollo.Messaging.Abstractions;
using Apollo.Messaging.NATS;
using Apollo.Messaging.Publishing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var config = ApolloConfig.Default;

var builder = Host.CreateApplicationBuilder(args);
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

var remoteDispatcher = publisherFactory.CreatePublisher("TestEndpoint");
await remoteDispatcher.BroadcastAsync(new TestEvent("Hello World!"), default);

//var response = await remoteDispatcher.SendRequestAsync<MyRequest,bool>(new MyRequest("TestRequest"), default);
//Console.WriteLine($"Response: {response}");
public record MyRequest(string Message) : IRequest<bool>;

public record TestEvent(string Message) : IEvent;