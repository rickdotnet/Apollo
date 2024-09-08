using Apollo.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Apollo.Extensions.Microsoft.Hosting;

public static class Startup
{
    public static IServiceCollection AddApollo(this IServiceCollection services, Action<IApolloBuilder> endpointBuilder)
        => services.AddApollo(new ApolloConfig(), endpointBuilder);
    
    public static IServiceCollection AddApollo(
        this IServiceCollection services,
        ApolloConfig apolloConfig,
        Action<IApolloBuilder>? builderAction
    )
    {
        services.TryAddSingleton(apolloConfig);

        var apolloBuilder = new ApolloBuilder(services);
        builderAction?.Invoke(apolloBuilder);
        
        apolloBuilder.Build();
        
        return services;
    }
}