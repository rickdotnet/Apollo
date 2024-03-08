using System.Text;
using Apollo;
using Apollo.Caching;
using Apollo.Configuration;
using Apollo.Hosting;
using Apollo.Messaging;
using Apollo.Messaging.Contracts;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using TestHost;

var config = new ApolloConfig("nats://nats.rhinostack.com:4222");

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(
        LogEventLevel.Debug,
        "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}")
    .CreateBootstrapLogger();

var builder = Host.CreateApplicationBuilder(args);
builder.Services
    .AddApollo(config, apolloBuilder =>
    {
        apolloBuilder
            .AddCaching()
            .WithEndpoints(
                endpoints =>
                {
                    endpoints.AddEndpoint<MyReplyEndpoint>();
                    endpoints.AddEndpoint<MyEndpoint>(cfg => cfg.DurableConfig.IsDurableConsumer = true);
                    endpoints.AddEndpoint<MyOtherEndpoint>();
                });
        //.WithRemotePublishing();
    });

var host = builder.Build();
//
// var publisherFactory = host.Services.GetRequiredService<IRemotePublisherFactory>();
// var publisher = publisherFactory.CreatePublisher("MyEndpoint");
// await publisher.BroadcastAsync(new TestEvent("My Event"), default);

// var cache = host.Services.GetRequiredService<IDistributedCache>();
// await cache.SetAsync("my-key", "my-value"u8.ToArray(), new DistributedCacheEntryOptions
// {
//     AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10)
// });
//
// var value = cache.Get("my-key");
// Console.WriteLine($"[GET] {Encoding.UTF8.GetString(value!)}");

await host.RunAsync();