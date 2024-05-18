namespace Apollo.Messaging.Abstractions;

public interface ILocalPublisher : IPublisher;
public interface IRemotePublisher : IPublisher;

public interface IPublisher
{
    string Route { get; }
    bool IsLocalOnly { get; }

    Task SendCommandAsync<TCommand>(TCommand commandMessage, CancellationToken cancellationToken)
        where TCommand : ICommand;

    Task BroadcastAsync<TEvent>(TEvent eventMessage, CancellationToken cancellationToken)
        where TEvent : IEvent;

    Task<TResponse?> SendRequestAsync<TRequest, TResponse>(TRequest requestMessage,
        CancellationToken cancellationToken)
        where TRequest : IRequest<TResponse>;
    
    Task SendObjectAsync(string subject, object commandMessage, CancellationToken cancellationToken);
    
    Task<object?> SendRequestAsync(string subject, object requestMessage, CancellationToken cancellationToken);
    Task<TResponse?> SendRequestAsync<TResponse>(string subject, object requestMessage, CancellationToken cancellationToken);
}