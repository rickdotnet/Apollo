// using System.Text;
// using Apollo;
// using Apollo.Abstractions;
// using Apollo.Configuration;
// using Apollo.Providers.NATS;
// using Microsoft.Extensions.DependencyInjection;
// using Microsoft.Extensions.Hosting;
// using NATS.Client.Core;
// using Serilog;
//
// namespace ConsoleDemo.Demo;
//
// public static class OriginalDemo
// {
//     public static async Task Demo()
//     {
//         var host = DemoUtil.CreateHost(addProvider: false); // add NATS or na?
//         using var scope = host.Services.CreateScope();
//
//         var serviceProvider = scope.ServiceProvider;
//         var apollo = serviceProvider.GetRequiredService<ApolloClient>();
//
//         var endpoint = DemoUtil.AddEndpoint<TestEndpoint>(apollo);
//         _ = endpoint.StartEndpoint(CancellationToken.None);
//
//         var anonEndpoint = DemoUtil.AnonEndpoint(apollo);
//         _ = anonEndpoint.StartEndpoint(CancellationToken.None);
//
//         await Task.Delay(3000);
//
//         var publisher = apollo.CreatePublisher(DemoUtil.PublishConfig);
//         await publisher.BroadcastAsync(new TestEvent("test message"), CancellationToken.None);
//         await publisher.BroadcastAsync(new TestEvent("test message"), CancellationToken.None);
//         await publisher.BroadcastAsync(new TestEvent("test message"), CancellationToken.None);
//         await publisher.BroadcastAsync(new TestEvent("test message"), CancellationToken.None);
//
//         Console.WriteLine("Press any key to exit");
//         Console.ReadKey();
//     }
//
//     static class DemoUtil
//     {
//         public static readonly ApolloConfig ApolloConfig = new()
//         {
//             DefaultConsumerName = "testconsumer",
//             DefaultNamespace = "dev.myapp",
//             Username = "console",
//             Password = "console"
//         };
//
//         public static readonly EndpointConfig EndpointConfig = new()
//         {
//             Namespace = "dev.myapp", // optional prefix for isolation
//             EndpointName = "My Endpoint", // slugified if no subject is provided (my-endpoint)
//             ConsumerName = "unique-to-me", // required for load balancing and durable scenarios
//             IsDurable = false // marker for subscription providers
//         };
//
//         public static readonly PublishConfig PublishConfig = new()
//         {
//             Namespace = "dev.myapp", // optional prefix for isolation
//             EndpointName = "My Endpoint", // slugified if no subject is provided (my-endpoint)
//             ProviderPublisher = null // optional provider publisher
//         };
//
//         public static IHost CreateHost(bool addProvider = false)
//         {
//             var builder = Host.CreateApplicationBuilder();
//             builder.Services.AddSerilog();
//             builder.Services
//                 .AddApollo(ApolloConfig);
//
//             if (addProvider)
//             {
//                 builder.Services
//                     .AddNatsProvider(opts => opts with
//                     {
//                         Url = "nats://localhost:4222",
//                         AuthOpts = new NatsAuthOpts
//                         {
//                             Username = ApolloConfig.Username,
//                             Password = ApolloConfig.Password
//                         }
//                     });
//             }
//
//             builder.Services.AddScoped<TestEndpoint>();
//             return builder.Build();
//         }
//
//         public static IApolloEndpoint AddEndpoint<T>(ApolloClient apollo)
//             => apollo.AddEndpoint<TestEndpoint>(EndpointConfig);
//
//         public static IApolloEndpoint AnonEndpoint(ApolloClient apollo)
//         {
//             var anonEndpoint = apollo.AddHandler(
//                 EndpointConfig with { EndpointSubject = "my-endpoint.testevent" },
//                 async (context, token) =>
//                 {
//                     Log.Debug("Entering anonymous handler");
//                     if (token.IsCancellationRequested)
//                         return;
//
//                     var message = context.Message;
//                     var stringMessage = Encoding.UTF8.GetString(message.Data!);
//                     Log.Information("Anonymous handler: {Message}", stringMessage);
//
//                     if (context.ReplyAvailable)
//                     {
//                         var response = "Hello from the other side"u8.ToArray();
//                         await context.ReplyAsync(response, token);
//                     }
//                 });
//
//             return anonEndpoint;
//         }
//     }
// }