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
#if DEBUG
        return new DefaultAzureCredential() { };
#else
        if (string.IsNullOrEmpty(asbConfig.ClientSecret))
            throw new Exception("Gonna be hard to get anywhere without some creds");
        
        // TODO: figure out which is which and then put it in asbConfig
        var options = new TokenCredentialOptions() { AuthorityHost = new Uri("https://login.windows.net") };
        return new ClientSecretCredential(asbConfig.TenantId, asbConfig.ClientId, asbConfig.ClientSecret, options);
        
        // // default with no options sets AuthorityHost to AzureAuthorityHosts.AzurePublicCloud => "https://login.microsoftonline.com/";
        // return new ClientSecretCredential(asbConfig.TenantId, asbConfig.ClientId, asbConfig.ClientSecret);
        // return new ClientSecretCredential();
#endif
    }
}