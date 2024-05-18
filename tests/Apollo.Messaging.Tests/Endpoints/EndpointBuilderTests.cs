using System.Collections;
using Apollo.Configuration;
using Apollo.Messaging.Endpoints;
using FakeItEasy;
using Microsoft.Extensions.DependencyInjection;

namespace Apollo.Messaging.Tests.Endpoints;

public class EndpointBuilderTests
{
    private readonly IServiceCollection services;
    private readonly ApolloConfig config = ApolloConfig.Default;
    private readonly EndpointBuilder endpointBuilder;

    public EndpointBuilderTests()
    {
        services = A.Fake<IServiceCollection>();
        endpointBuilder = new EndpointBuilder(services, config);
    }

    [Fact]
    public void AddEndpoint_ShouldCallAddScopedWithType()
    {
        endpointBuilder.AddEndpoint<SomeEndpoint>();

        A.CallTo(() => services.Add(
                A<ServiceDescriptor>.That.Matches(
                    sd => sd.ServiceType == typeof(SomeEndpoint) && sd.Lifetime == ServiceLifetime.Scoped)))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public void Build_ShouldAddRegistryToServiceCollection()
    {
       // endpointBuilder.AddEndpoint<SomeEndpoint>();
        endpointBuilder.Build();
        
        A.CallTo(() => services.Add(
                A<ServiceDescriptor>.That.Matches(
                    sd => sd.ServiceType == typeof(IEndpointRegistry) && sd.Lifetime == ServiceLifetime.Singleton)))
            .MustHaveHappenedOnceExactly();
    }
    
    private class SomeEndpoint {}
    private class SomeSubscriber : ISubscriber
    {
        public Task SubscribeAsync(SubscriptionConfig config, Func<ApolloMessage, CancellationToken, Task> handler, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}