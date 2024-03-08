using Apollo.Messaging.Endpoints;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Apollo.Messaging;

public static class Setup
{
    public static ApolloBuilder WithEndpoints(this ApolloBuilder apolloBuilder, Action<EndpointBuilder> builderAction)
    {
        var endpointBuilder = new EndpointBuilder(apolloBuilder.Services, apolloBuilder.Config);
        endpointBuilder.AddEndpoint<InternalEndpoint>(cfg => cfg.IsLocalEndpoint = true);
        builderAction(endpointBuilder);
        
        return apolloBuilder;
    }
    public static ApolloBuilder WithRemotePublishing(this ApolloBuilder apolloBuilder)
    {
        apolloBuilder.Services.TryAddSingleton<IRemotePublisherFactory, RemotePublisherFactory>();
        return apolloBuilder;
    }
}