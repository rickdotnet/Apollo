namespace Apollo.Abstractions;

public interface IApolloEndpoint : IAsyncDisposable
{
    Task StartEndpoint(CancellationToken cancellationToken);
    //Task HandleAsync(ApolloContext context, CancellationToken cancellationToken);
    // ValueTask DisposeAsync();
}