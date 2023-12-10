using Apollo.Core.Configuration;
using Apollo.Core.Hosting;
using Microsoft.Extensions.Hosting;
using TestHost;

var config = new ApolloConfig("nats://nats.rhinostack.com:4222");

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddApollo(config)
    .WithEndpoints(
        endpoints =>
        { 
            //endpoints.AddEndpoint<MyEndpoint>();
            //endpoints.AddEndpoint<MyOtherEndpoint>(cfg => cfg.IsLocalEndpoint = true);
            //endpoints.AddEndpoint<MyReplyEndpoint>(cfg=>cfg.DurableConfig.IsDurableConsumer = true);
            endpoints.AddEndpoint<MyEndpoint>(cfg=>cfg.DurableConfig.IsDurableConsumer = true);
            endpoints.AddEndpoint<MyOtherEndpoint>();
        });

var host = builder.Build();

await host.RunAsync();