namespace Apollo.Abstractions;

public interface IReplyTo<in TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    public Task<TResponse> Handle(TRequest message, CancellationToken cancellationToken = default);
}