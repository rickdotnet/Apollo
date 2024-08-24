using Apollo.Abstractions;
using Apollo.Configuration;
using Apollo.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Apollo;

public static class Setup
{
    public static IServiceCollection AddApollo(this IServiceCollection services, ApolloConfig? config = null)
    {
        if (config != null)
            services.TryAddSingleton(config);
        
        services.AddSingleton(sp => new ApolloClient(
            sp.GetService<ISubscriptionProvider>(), // default is set in constructor
            sp.GetService<IProviderPublisher>(), // default is set in constructor
            sp.GetService<IEndpointProvider>() ?? new DefaultEndpointProvider(sp)
        ));

        return services;
    }
}