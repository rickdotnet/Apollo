using Apollo;
using Apollo.Messaging;
using Apollo.Messaging.Endpoints;
using Microsoft.Extensions.Hosting;
using TestHost;

var builder = Host.CreateApplicationBuilder(args);
builder.Services
    .AddApollo(
        apolloBuilder =>
        {
            apolloBuilder
                .WithEndpoints(
                    endpoints =>
                    {
                        endpoints.AddEndpoint<MyReplyEndpoint>();
                        endpoints.AddEndpoint<MyEndpoint>(cfg => cfg.SetDurableConsumer());
                        endpoints.AddEndpoint<MyOtherEndpoint>();
                    });
        });

var host = builder.Build();
await host.RunAsync();