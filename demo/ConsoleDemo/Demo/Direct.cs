using Apollo;
using Apollo.Abstractions;
using Apollo.Configuration;

namespace ConsoleDemo.Demo;

#region docs-snippet

public static class Direct
{
    public static async Task Demo()
    {
        var config = new ApolloConfig();
        var endpoints = new EndpointProvider();
        var demo = new ApolloClient(config, endpointProvider: endpoints);
        var endpoint = demo.AddEndpoint<TestEndpoint>(TestEndpoint.EndpointConfig);
        _ = endpoint.StartEndpoint(CancellationToken.None);

        var publisher = demo.CreatePublisher(TestEndpoint.EndpointConfig);

        await Task.WhenAll([
            publisher.Broadcast(new TestEvent("test 1"), CancellationToken.None),
            publisher.Broadcast(new TestEvent("test 2"), CancellationToken.None),
            publisher.Broadcast(new TestEvent("test 3"), CancellationToken.None),
            publisher.Broadcast(new TestEvent("test 4"), CancellationToken.None),
            publisher.Broadcast(new TestEvent("test 5"), CancellationToken.None)
        ]);

        Console.WriteLine("Press any key to exit");
        Console.ReadKey();

        await endpoint.DisposeAsync();
    }

    class EndpointProvider : IEndpointProvider
    {
        private readonly TestEndpoint testEndpoint = new();
        public object? GetService(Type endpointType) => testEndpoint;
    }
}

#endregion