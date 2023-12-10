// See https://aka.ms/new-console-template for more information

using Apollo.Core;
using Apollo.Core.Configuration;
using Apollo.Core.Hosting;
using Apollo.Core.Messaging.Events;
using Apollo.Core.Messaging.Requests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var config = new ApolloConfig("nats://nats.rhinostack.com:4222");

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddApollo(config)
    .WithRemotePublishing();
    //.WithRemotePublisher("MyEndpoint");

var host = builder.Build();
var publisherFactory = host.Services.GetRequiredService<IRemotePublisherFactory>();

//var remoteDispatcher = publisherFactory.CreatePublisher("MyEndpoint");
var remoteDispatcher = publisherFactory.CreatePublisher("MyReplyEndpoint");

Console.WriteLine("Sending remote event...");

//await remoteDispatcher.BroadcastAsync(new TestMessage("Remote Test Event"), CancellationToken.None);
var test = await remoteDispatcher.SendRequestAsync<MyRequest,bool>(new MyRequest("test"), CancellationToken.None);
Console.WriteLine($"Got response: {test}");

Console.WriteLine("Sent remote event");
await Task.Delay(5000);

Console.WriteLine("Closing");

//await host.RunAsync();
public record TestMessage(string Message) : IEvent
{
    public string Message { get; set; } = Message;
}
public record MyRequest(string Message) : IRequest<bool>;