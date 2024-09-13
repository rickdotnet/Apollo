using Apollo.Abstractions;

namespace Apollo.Providers.ASB;

internal class AsbPublisher : IPublisher
{
    public Task Send<TCommand>(TCommand commandMessage, CancellationToken cancellationToken) where TCommand : ICommand
    {
        throw new NotImplementedException();
    }

    public Task Broadcast<TEvent>(TEvent eventMessage, CancellationToken cancellationToken) where TEvent : IEvent
    {
        throw new NotImplementedException();
    }

    public Task<TResponse?> Request<TRequest, TResponse>(TRequest requestMessage, CancellationToken cancellationToken) where TRequest : IRequest<TResponse>
    {
        throw new NotImplementedException();
    }
}