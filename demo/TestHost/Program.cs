using Apollo;
using Apollo.Configuration;
using Apollo.Messaging;
using Apollo.Messaging.Azure;
using Apollo.Messaging.Endpoints;
using Apollo.Messaging.NATS;
using Microsoft.Extensions.Hosting;
using TestHost;

var builder = Host.CreateApplicationBuilder(args);
var config = new ApolloConfig() { CreateMissingResources = true};
builder.Services
    .AddApollo(
        config,
        apolloBuilder =>
        {
            apolloBuilder
                .UseNats()
                .UseAzure()
                .WithEndpoints(
                    endpoints =>
                    {
                        endpoints
                            //.AddEndpoint<MyEndpoint>()
                            .AddEndpoint<MyEndpoint>(cfg => cfg.SetDurableConsumer())
                            .AddEndpoint<MyOtherEndpoint>(cfg => cfg.SetLocalOnly())
                            .AddEndpoint<MyReplyEndpoint>()
                        .AddSubscriber<AzureServiceBusSubscriber>()
                        .AddSubscriber<NatsSubscriber>();
                    });
        });

var host = builder.Build();

await host.RunAsync();