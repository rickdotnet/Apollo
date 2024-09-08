namespace Apollo.Abstractions;

public interface IPublisher
{
    Task SendAsync<TCommand>(TCommand commandMessage, CancellationToken cancellationToken)
        where TCommand : ICommand;

    Task BroadcastAsync<TEvent>(TEvent eventMessage, CancellationToken cancellationToken)
        where TEvent : IEvent;

    Task<TResponse?> RequestAsync<TRequest, TResponse>(TRequest requestMessage,
        CancellationToken cancellationToken)
        where TRequest : IRequest;
}