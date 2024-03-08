using Apollo.Abstractions.Messaging;
using Apollo.Abstractions.Messaging.Commands;
using Apollo.Abstractions.Messaging.Events;
using Apollo.Abstractions.Messaging.Requests;
using Microsoft.Extensions.Logging;

namespace Apollo.Messaging;

public interface ILocalPublisher : IPublisher;

internal class LocalPublisher : ILocalPublisher
{
    private readonly ILogger<LocalPublisher> logger;
    private readonly IApolloDispatcher dispatcher;

    public LocalPublisher(ILogger<LocalPublisher> logger, IApolloDispatcher dispatcher)
    {
        this.logger = logger;
        this.dispatcher = dispatcher;
    }
    public Task SendCommandAsync<TCommand>(TCommand commandMessage,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand
    {
        logger.LogInformation("Sending command {Name}", typeof(TCommand).Name);
        return dispatcher.SendCommandToLocalEndpointsAsync(commandMessage, cancellationToken);
    }

    public Task BroadcastAsync<TEvent>(TEvent eventMessage,
        CancellationToken cancellationToken = default)
        where TEvent : IEvent
    {
        logger.LogInformation("Broadcasting event {Name}", typeof(TEvent).Name);
        return dispatcher.BroadcastToLocalEndpointsAsync(eventMessage, cancellationToken);
    }

    public Task<TResponse> SendRequestAsync<TRequest, TResponse>(
        TRequest requestMessage, CancellationToken cancellationToken)
        where TRequest : IRequest<TResponse>
    {
        logger.LogInformation("Sending request {Name}", typeof(TRequest).Name);
        return dispatcher.SendRequestToLocalEndpointsAsync<TRequest, TResponse>(requestMessage, cancellationToken);
    }
}