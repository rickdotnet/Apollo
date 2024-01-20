using Apollo.Configuration;
using Apollo.Hosting;
using Microsoft.Extensions.Hosting;
using TestHost;

var config = new ApolloConfig();

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddApollo(config)
    .WithEndpoints(
        endpoints =>
        { 
            endpoints.AddEndpoint<MyReplyEndpoint>();
            endpoints.AddEndpoint<MyEndpoint>(cfg=>cfg.DurableConfig.IsDurableConsumer = true);
            endpoints.AddEndpoint<MyOtherEndpoint>();
        });

var host = builder.Build();

await host.RunAsync();