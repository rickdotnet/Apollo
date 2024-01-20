using Apollo.Abstractions.Messaging.Commands;
using Apollo.Abstractions.Messaging.Events;
using Apollo.Abstractions.Messaging.Requests;
using Apollo.Core.Endpoints;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Apollo.Core.Messaging;

public interface ILocalPublisher : IPublisher;

public class LocalPublisher(IServiceProvider serviceProvider) : ILocalPublisher
{
    private readonly ILogger<LocalPublisher> logger = serviceProvider.GetRequiredService<ILogger<LocalPublisher>>();
    private readonly IApolloDispatcher dispatcher = serviceProvider.GetRequiredService<IApolloDispatcher>();

    public ValueTask SendCommandAsync<TCommand>(TCommand commandMessage,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand
    {
        logger.LogInformation("Sending command {Name}", typeof(TCommand).Name);
        return dispatcher.SendCommandToLocalEndpointsAsync(commandMessage, cancellationToken);
    }

    public ValueTask BroadcastAsync<TEvent>(TEvent eventMessage,
        CancellationToken cancellationToken = default)
        where TEvent : IEvent
    {
        logger.LogInformation("Broadcasting event {Name}", typeof(TEvent).Name);
        return dispatcher.BroadcastToLocalEndpointsAsync(eventMessage, cancellationToken);
    }

    public ValueTask<TResponse> SendRequestAsync<TRequest, TResponse>(
        TRequest requestMessage, CancellationToken cancellationToken)
        where TRequest : IRequest<TResponse>
    {
        logger.LogInformation("Sending request {Name}", typeof(TRequest).Name);
        return dispatcher.SendRequestToLocalEndpointsAsync<TRequest, TResponse>(requestMessage, cancellationToken);
    }
}