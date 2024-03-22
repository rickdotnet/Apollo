using Apollo.Messaging.Endpoints;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Apollo.Messaging;

public static class Setup
{
    public static ApolloBuilder WithEndpoints(this ApolloBuilder apolloBuilder, Action<IEndpointBuilder>? builderAction = null)
    {
        var endpointBuilder = new EndpointBuilder(apolloBuilder.Services, apolloBuilder.Config);
        builderAction?.Invoke(endpointBuilder);

        apolloBuilder.Services.TryAddSingleton<IPublisherFactory, PublisherFactory>();
        //apolloBuilder.Services.TryAddTransient<LocalPublisher>();
        return apolloBuilder;
    }
}