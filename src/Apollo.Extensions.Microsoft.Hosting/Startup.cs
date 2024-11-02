using Microsoft.Extensions.DependencyInjection;

namespace Apollo.Extensions.Microsoft.Hosting;

public static class Startup
{
    public static IServiceCollection AddApollo(
        this IServiceCollection services,
        Action<IApolloBuilder>? builderAction
    )
    {
        var apolloBuilder = new ApolloBuilder(services);
        builderAction?.Invoke(apolloBuilder);
        
        apolloBuilder.Build();
        
        return services;
    }
}