using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Apollo.Messaging.Azure;

public static class Setup
{
    public static ApolloBuilder UseAzure(
        this ApolloBuilder apolloBuilder)
    {
        apolloBuilder.Services.TryAddSingleton<AzureServiceBusSubscriber>();
        apolloBuilder.Services.AddSingleton<ISubscriber>(x => x.GetRequiredService<AzureServiceBusSubscriber>());

        // TODO: Take this out and clean it from git before pushing
        var connectionString = "<connection-string>";
        apolloBuilder.Services.AddSingleton(new ServiceBusClient(connectionString));
        apolloBuilder.Services.AddSingleton(new ServiceBusAdministrationClient(connectionString));
        apolloBuilder.Services.AddSingleton<BusResourceManager>();
        return apolloBuilder;
    }
}