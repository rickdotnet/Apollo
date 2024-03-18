using Apollo;
using Apollo.Messaging;
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
                        endpoints.AddEndpoint<MyEndpoint>(cfg => cfg.DurableConfig.IsDurableConsumer = true);
                        endpoints.AddEndpoint<MyOtherEndpoint>();
                    });
        });

var host = builder.Build();
await host.RunAsync();