using Apollo.Abstractions;

namespace Apollo.Internal;

internal class DefaultEndpointProvider(IServiceProvider provider) : IEndpointProvider
{
    public object? GetService(Type endpointType) => provider.GetService(endpointType);
}