using Apollo.Abstractions;
using Apollo.Configuration;
using Apollo.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Apollo;

public static class Setup
{
    public static IServiceCollection AddApollo(this IServiceCollection services, ApolloConfig? apolloConfig = null)
    {
        var config = apolloConfig ?? new();
        services.TryAddSingleton(config);
        services.TryAddSingleton<IEndpointProvider, DefaultEndpointProvider>();
        services.AddSingleton<ApolloClient>();

        return services;
    }
}