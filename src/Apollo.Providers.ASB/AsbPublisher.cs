using Apollo.Abstractions;

namespace Apollo.Providers.ASB;

public class AsbPublisher : IPublisher
{
    public Task SendAsync<TCommand>(TCommand commandMessage, CancellationToken cancellationToken) where TCommand : ICommand
    {
        throw new NotImplementedException();
    }

    public Task BroadcastAsync<TEvent>(TEvent eventMessage, CancellationToken cancellationToken) where TEvent : IEvent
    {
        throw new NotImplementedException();
    }

    public Task<TResponse?> RequestAsync<TRequest, TResponse>(TRequest requestMessage, CancellationToken cancellationToken) where TRequest : IRequest<TResponse>
    {
        throw new NotImplementedException();
    }
}