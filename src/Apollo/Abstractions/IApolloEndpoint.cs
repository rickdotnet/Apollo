namespace Apollo.Abstractions;

public interface IApolloEndpoint : IAsyncDisposable
{
    Task StartEndpoint(CancellationToken cancellationToken);
}