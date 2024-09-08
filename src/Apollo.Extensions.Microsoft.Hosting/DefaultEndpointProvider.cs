using Apollo.Abstractions;

namespace Apollo.Extensions.Microsoft.Hosting;

internal class DefaultEndpointProvider(IServiceProvider provider) : IEndpointProvider
{
    public object? GetService(Type endpointType) => provider.GetService(endpointType);
}