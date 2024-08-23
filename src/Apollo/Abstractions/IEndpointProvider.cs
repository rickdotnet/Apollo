namespace Apollo.Abstractions;

public interface IEndpointProvider
{
    object? GetService(Type endpointType);
}