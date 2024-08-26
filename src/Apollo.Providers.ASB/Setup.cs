using Apollo.Abstractions;
using Apollo.Configuration;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Apollo.Providers.ASB;

public static class Setup
{
    [Obsolete("The ASB provider is not fully implemented .", false)]
    public static IServiceCollection AddAsbProvider(this IServiceCollection services, AsbConfig asbConfig)
    {
        services.AddSingleton(asbConfig);
        services.AddSingleton(new BusResourceManager(asbConfig.ConnectionString, GetCreds(asbConfig)));
        services.AddSingleton<AsbSubscriptionProvider>();
        services.AddSingleton<ISubscriptionProvider>(x => x.GetRequiredService<AsbSubscriptionProvider>());
        
        
        return services;
    }

    private static TokenCredential GetCreds(AsbConfig asbConfig)
    {
        // local dev
        return new DefaultAzureCredential() { };
    }
}