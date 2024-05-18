using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Apollo.Messaging.ASB;

public static class Setup
{
    public static ApolloBuilder UseASB(
        this ApolloBuilder apolloBuilder)
    {
       apolloBuilder.Services.TryAddSingleton<AzureServiceBusSubscriber>();
       apolloBuilder.Services.AddSingleton<ISubscriber>(x => x.GetRequiredService<AzureServiceBusSubscriber>());

        return apolloBuilder;
    }
}