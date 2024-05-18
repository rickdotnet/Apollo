using Apollo;
using Apollo.Messaging;
using Apollo.Messaging.Endpoints;
using Apollo.Messaging.NATS;
using Microsoft.Extensions.Hosting;
using TestHost;

var builder = Host.CreateApplicationBuilder(args);
builder.Services
    .AddApollo(
        apolloBuilder =>
        {
            apolloBuilder
                .UseNats()
                .WithEndpoints(
                    endpoints =>
                    {
                        endpoints
                            .AddEndpoint<MyReplyEndpoint>()
                            .AddEndpoint<MyEndpoint>(cfg => cfg.SetDurableConsumer())
                            .AddEndpoint<MyOtherEndpoint>()
                            .AddSubscriber<NatsSubscriber>();
                    });
        });

var host = builder.Build();

await host.RunAsync();