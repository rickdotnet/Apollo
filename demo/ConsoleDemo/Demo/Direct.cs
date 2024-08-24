using Apollo;
using Apollo.Abstractions;
using Apollo.Configuration;

namespace ConsoleDemo.Demo;

public static class Direct
{
    public static async Task Demo()
    {
        var endpointConfig = new EndpointConfig
        {
            Namespace = "dev.myapp", // optional prefix for isolation
            EndpointName = "My Endpoint", // slugified if no subject is provided (my-endpoint)
            ConsumerName = "unique-to-me", // required for load balancing and durable scenarios
            IsDurable = false // marker for subscription providers
        };

        var config = new ApolloConfig();
        var endpoints = new EndpointProvider();
        var demo = new ApolloClient(config, endpointProvider: endpoints );
        var endpoint = demo.AddEndpoint<TestEndpoint>(endpointConfig);
        _ = endpoint.StartEndpoint(CancellationToken.None);

        var publisher = demo.CreatePublisher(endpointConfig);

        await Task.WhenAll([
            publisher.BroadcastAsync(new TestEvent("test 1"), CancellationToken.None),
            publisher.BroadcastAsync(new TestEvent("test 2"), CancellationToken.None),
            publisher.BroadcastAsync(new TestEvent("test 3"), CancellationToken.None),
            publisher.BroadcastAsync(new TestEvent("test 4"), CancellationToken.None),
            publisher.BroadcastAsync(new TestEvent("test 5"), CancellationToken.None)
        ]);

        Console.WriteLine("Press any key to exit");
        Console.ReadKey();
    }
    class EndpointProvider : IEndpointProvider
    {
        private readonly TestEndpoint testEndpoint = new();
        public object? GetService(Type endpointType) => testEndpoint;
    }
}