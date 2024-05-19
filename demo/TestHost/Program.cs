using Apollo;
using Apollo.Messaging;
using Apollo.Messaging.Azure;
using Apollo.Messaging.Endpoints;
using Microsoft.Extensions.Hosting;
using TestHost;

var builder = Host.CreateApplicationBuilder(args);
builder.Services
    .AddApollo(
        apolloBuilder =>
        {
            apolloBuilder
                //.UseNats()
                .UseAzure()
                .WithEndpoints(
                    endpoints =>
                    {
                        endpoints
                            //.AddEndpoint<MyEndpoint>()
                            .AddEndpoint<MyEndpoint>(cfg => cfg.SetDurableConsumer())
                            .AddEndpoint<MyOtherEndpoint>()
                            .AddEndpoint<MyReplyEndpoint>();
                            //.AddEndpoint<MyReplyEndpoint>(cfg => cfg.SetLocalOnly());
                        //.AddSubscriber<AzureServiceBusSubscriber>();
                    });
        });

var host = builder.Build();

await host.RunAsync();