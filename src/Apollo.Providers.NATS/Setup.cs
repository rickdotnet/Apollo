using Apollo.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using NATS.Client.Core;
using NATS.Extensions.Microsoft.DependencyInjection;

namespace Apollo.Providers.NATS;

public static class Setup
{
    public static IServiceCollection AddNatsProvider(this IServiceCollection services, Func<NatsOpts, NatsOpts> configureOptions)
    {
        services.AddNatsClient(
            nats => nats.ConfigureOptions(
                opts => configureOptions(opts)
            )
        );

        services.AddSingleton<ISubscriptionProvider, NatsSubscriptionProvider>();
        services.AddSingleton<IProviderPublisher, NatsPublisher>();
        return services;
    }
}