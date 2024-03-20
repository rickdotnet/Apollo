namespace Apollo.Messaging.Abstractions;

public interface IReplyTo : IMessage
{
}

public interface IReplyTo<in TRequest, TResponse> : IReplyTo where TRequest : IRequest<TResponse>
{
    public Task<TResponse> HandleAsync(TRequest message, CancellationToken cancellationToken = default);
}