using Apollo;
using Apollo.Caching;
using Apollo.Configuration;
using Apollo.Messaging;
using Microsoft.Extensions.Hosting;
using TestHost;

var config = new ApolloConfig("nats://nats.rhinostack.com:4222");
config.DefaultNamespace = "apollo.cm";

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
    });



var host = builder.Build();

// var cache = host.Services.GetRequiredService<IDistributedCache>();
// await cache.SetAsync("my-key", "my-value"u8.ToArray(), new DistributedCacheEntryOptions
// {
//     AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10)
// });
//
// var value = cache.Get("my-key");
// Console.WriteLine($"[GET] {Encoding.UTF8.GetString(value!)}");

await host.RunAsync();