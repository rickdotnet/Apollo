using Apollo.Configuration;
using Apollo.Messaging.Abstractions;
using Apollo.Messaging.Contracts;
using Apollo.Messaging.Endpoints;

namespace Apollo.Messaging.Tests.Endpoints;

public class EndpointRegistryTests
{
    private readonly EndpointRegistry endpointRegistry = new();

    [Fact]
    public void RegisterEndpoint_ShouldAddEndpointRegistration()
    {
        var config = EndpointConfig.Default;
        var registration = new EndpointRegistration<SomeEndpoint>(config);

        endpointRegistry.RegisterEndpoint(registration);

        var endpoints = endpointRegistry.GetEndpointRegistrations();
        Assert.Contains(registration, endpoints);
    }
    
    [Fact]
    public void AddSubscriberType_ShouldAddSubscriberToList()
    {
        endpointRegistry.AddSubscriberType<SomeSubscriber>();

        Assert.Contains(typeof(SomeSubscriber), endpointRegistry.SubscriberTypes);
    }

    private class SomeEndpoint : IListenFor<TestEvent>
    {
        public Task HandleAsync(TestEvent message, CancellationToken cancellationToken = default) 
            => Task.CompletedTask;
    }
    
    private class SomeSubscriber : ISubscriber
    {
        public Task SubscribeAsync(SubscriptionConfig config, Func<ApolloMessage, CancellationToken, Task> handler, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}