using Apollo.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NATS.Client.Core;
using NATS.Extensions.Microsoft.DependencyInjection;

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
        services.AddSingleton(config);
        services.AddNatsClient(
            nats => nats.ConfigureOptions(ops => ops with
            {
                Url = config.Url,
                ConnectTimeout = TimeSpan.FromSeconds(10),
                RequestTimeout = TimeSpan.FromSeconds(10),
                AuthOpts = NatsAuthOpts.Default with
                {
                    CredsFile = config.CredsFile,
                    Token = config.Token,
                    NKey = config.NKey,
                    Seed = config.Seed,
                    Jwt = config.Jwt,
                }
            }));

        var builder = new ApolloBuilder(services, config);
        builderAction?.Invoke(builder);

        return services;
    }

    public static ILogger GetLogger<T>(this IServiceProvider serviceProvider)
        => serviceProvider.GetService<ILogger<T>>()
           ?? (ILogger)NullLogger.Instance;
}