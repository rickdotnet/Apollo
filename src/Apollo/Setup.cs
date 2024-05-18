using Apollo.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Apollo;

public static class Setup
{
    public static IServiceCollection AddApollo(this IServiceCollection services)
        => AddApollo(services, ApolloConfig.Default);

    public static IServiceCollection AddApollo(this IServiceCollection services, Action<ApolloBuilder> builderAction)
        => AddApollo(services, ApolloConfig.Default, builderAction);

    public static IServiceCollection AddApollo(this IServiceCollection services, ApolloConfig config,
        Action<ApolloBuilder>? builderAction = null)
    {
        services.TryAddSingleton(config);

        var builder = new ApolloBuilder(services, config);
        builderAction?.Invoke(builder);

        return services;
    }
}