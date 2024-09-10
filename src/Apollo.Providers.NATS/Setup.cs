using Apollo.Abstractions;
using Apollo.Extensions.Microsoft.Hosting;
using Microsoft.Extensions.DependencyInjection;
using NATS.Client.Core;
using NATS.Extensions.Microsoft.DependencyInjection;

namespace Apollo.Providers.NATS;

public static class Setup
{
    /// <summary>
    /// Adds the NATS provider to the Apollo builder.
    /// </summary>
    /// <param name="builder">The Apollo builder.</param>
    /// <param name="configureOptions">The configuration options.</param>
    /// <returns>The builder instance.</returns>
    /// <remarks>This method will add a NATS client to the service collection.</remarks>
    public static IApolloBuilder AddNatsProvider(this IApolloBuilder builder, Func<NatsOpts, NatsOpts> configureOptions)
    {
        builder.Services.AddNatsClient(nats => nats.ConfigureOptions(configureOptions));

        builder.Services.AddSingleton<ISubscriptionProvider, NatsSubscriptionProvider>();
        builder.Services.AddSingleton<IProviderPublisher, NatsPublisher>();
        return builder;
    }
}