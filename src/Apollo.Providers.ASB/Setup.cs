using Apollo.Abstractions;
using Apollo.Extensions.Microsoft.Hosting;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Apollo.Providers.ASB;

public static class Setup
{
    [Obsolete("The ASB provider is not fully implemented .", false)]
    public static IApolloBuilder AddAsbProvider(this IApolloBuilder builder, AsbConfig asbConfig)
    {
        builder.Services.AddSingleton(asbConfig);
        builder.Services.AddSingleton(new BusResourceManager(asbConfig.ConnectionString, GetCreds(asbConfig)));
        builder.Services.AddSingleton<AsbSubscriptionProvider>();
        builder.Services.AddSingleton<ISubscriptionProvider>(x => x.GetRequiredService<AsbSubscriptionProvider>());
        
        
        return builder;
    }

    private static TokenCredential GetCreds(AsbConfig asbConfig)
    {
        // local dev
        return new DefaultAzureCredential() { };
    }
}