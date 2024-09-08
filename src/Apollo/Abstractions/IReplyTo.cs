namespace Apollo.Abstractions;

public interface IReplyTo<in TRequest, TResponse> where TRequest : IRequest
{
    public Task<TResponse> HandleAsync(TRequest message, CancellationToken cancellationToken = default);
}