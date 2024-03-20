using Apollo;
using Apollo.Configuration;
using Apollo.Messaging;
using Apollo.Messaging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var config = ApolloConfig.Default;

var builder = Host.CreateApplicationBuilder(args);
builder.Services
    .AddApollo(config, x=>x.WithEndpoints());

var host = builder.Build();
var publisherFactory = host.Services.GetRequiredService<IPublisherFactory>();

var remoteDispatcher = publisherFactory.CreatePublisher("MyReplyEndpoint");
var response = await remoteDispatcher.SendRequestAsync<MyRequest,bool>(new MyRequest("My Test Request"), default);
Console.WriteLine("Response: " + response);

public record MyRequest(string Message) : IRequest<bool>;