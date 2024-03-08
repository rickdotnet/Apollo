namespace Apollo.Abstractions.Messaging.Requests;

public interface IReplyTo : IMessage
{
}

public interface IReplyTo<in TRequest, TResponse> : IReplyTo where TRequest : IRequest<TResponse>
{
    public Task<TResponse> HandleRequestAsync(TRequest message, CancellationToken cancellationToken = default);
}