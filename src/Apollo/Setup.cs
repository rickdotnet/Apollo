using Apollo.Abstractions;
using Apollo.Configuration;
using Apollo.Internal;
using Apollo.Providers.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Apollo;

public static class Setup
{
    public static IServiceCollection AddApollo(this IServiceCollection services, ApolloConfig config)
    {
        services.TryAddSingleton(config);
        services.AddSingleton(sp => new ApolloClient(
            sp.GetService<ISubscriptionProvider>() ?? InMemoryProvider.Instance,
            sp.GetService<IProviderPublisher>() ?? InMemoryProvider.Instance,
            sp.GetService<IEndpointProvider>() ?? new DefaultEndpointProvider(sp)
            ));

        return services;
    }
}