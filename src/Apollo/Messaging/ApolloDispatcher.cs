using Apollo.Abstractions.Messaging;
using Apollo.Abstractions.Messaging.Commands;
using Apollo.Abstractions.Messaging.Events;
using Apollo.Abstractions.Messaging.Requests;
using Apollo.Endpoints;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Apollo.Messaging;

public class ApolloDispatcher : IApolloDispatcher
{
    private readonly ILogger<ApolloDispatcher> logger;
    private readonly IEndpointRegistry endpointRegistry;
    private readonly IServiceProvider serviceProvider;

    public ApolloDispatcher(IServiceProvider serviceProvider)
    {
        logger = serviceProvider.GetRequiredService<ILogger<ApolloDispatcher>>();
        endpointRegistry = serviceProvider.GetRequiredService<IEndpointRegistry>();
        this.serviceProvider = serviceProvider;
    }

    public ValueTask<TResponse> SendRequestToLocalEndpointsAsync<TRequest, TResponse>(TRequest requestMessage,
        CancellationToken cancellationToken)
        where TRequest : IRequest<TResponse>
    {
        return SendRequestInternalAsync<TRequest, TResponse>(requestMessage, false, cancellationToken);
    }

    public ValueTask<TResponse> SendRequestToRemoteEndpointsAsync<TRequest, TResponse>(TRequest requestMessage,
        CancellationToken cancellationToken)
        where TRequest : IRequest<TResponse>
    {
        return SendRequestInternalAsync<TRequest, TResponse>(requestMessage, true, cancellationToken);
    }

    public ValueTask SendCommandToLocalEndpointsAsync<TCommand>(TCommand commandMessage,
        CancellationToken cancellationToken)
        where TCommand : ICommand
    {
        return SendCommandInternalAsync(commandMessage, false, cancellationToken);
    }

    public ValueTask SendCommandToRemoteEndpointsAsync<TCommand>(TCommand commandMessage,
        CancellationToken cancellationToken)
        where TCommand : ICommand
    {
        return SendCommandInternalAsync(commandMessage, true, cancellationToken);
    }

    public ValueTask SendCommandToSingleRemoteEndpointsAsync<TCommand>(EndpointRegistration registration, TCommand commandMessage,
        CancellationToken cancellationToken) where TCommand : ICommand
    {
        return DispatchToHandler(registration, commandMessage, cancellationToken);
    }

    public ValueTask BroadcastToLocalEndpointsAsync<TEvent>(TEvent eventMessage,
        CancellationToken cancellationToken)
        where TEvent : IEvent
    {
        return BroadcastInternalAsync(eventMessage, false, cancellationToken);
    }

    public ValueTask BroadcastToRemoteEndpointsAsync<TEvent>(TEvent eventMessage,
        CancellationToken cancellationToken)
        where TEvent : IEvent
    {
        return BroadcastInternalAsync(eventMessage, true, cancellationToken);
    }

    public ValueTask BroadcastToSingleRemoteEndpointAsync<TEvent>(EndpointRegistration registration, TEvent eventMessage,
        CancellationToken cancellationToken) where TEvent : IEvent
    {
        return DispatchToHandler(registration, eventMessage, cancellationToken);
    }

    private async ValueTask<TResponse> SendRequestInternalAsync<TRequest, TResponse>(TRequest requestMessage,
        bool toRemote, CancellationToken cancellationToken)
        where TRequest : IRequest<TResponse>
    {
        var endpoints = FilterEndpoints<TRequest>(toRemote, endpointRegistry.GetEndpointsForRequest<TRequest>)
            .ToArray();

        if (endpoints.Length == 0)
            throw new InvalidOperationException($"No endpoints registered for request {typeof(TRequest).Name}");

        if (endpoints.Length > 1)
            throw new InvalidOperationException(
                $"Multiple endpoints registered for request {typeof(TRequest).Name}");

        var endpointInstance = serviceProvider.GetRequiredService(endpoints.First().EndpointType);

        if (endpointInstance is IReplyTo<TRequest, TResponse> requestHandler)
        {
            return await requestHandler.HandleRequestAsync(requestMessage, cancellationToken);
        }

        throw new InvalidOperationException(
            $"Endpoint {endpointInstance.GetType().Name} does not implement IReplyTo<{typeof(TRequest).Name}, {typeof(TResponse).Name}>");
    }

    private async ValueTask SendCommandInternalAsync<TCommand>(TCommand commandMessage, bool toRemote,
        CancellationToken cancellationToken)
        where TCommand : ICommand
    {
        var endpoints = FilterEndpoints<TCommand>(toRemote, endpointRegistry.GetEndpointsForCommand<TCommand>)
            .ToArray();

        if (endpoints.Length == 0)
            throw new InvalidOperationException($"No endpoints registered for command {typeof(TCommand).Name}");

        if (endpoints.Length > 1)
            throw new InvalidOperationException(
                $"Multiple endpoints registered for command {typeof(TCommand).Name}");

        await DispatchToHandler(endpoints.First(), commandMessage, cancellationToken);
    }

    private async ValueTask BroadcastInternalAsync<TEvent>(TEvent eventMessage, bool toRemote,
        CancellationToken cancellationToken)
        where TEvent : IEvent
    {
        var endpoints = FilterEndpoints<TEvent>(toRemote, endpointRegistry.GetEndpointsForEvent<TEvent>);

        foreach (var endpointRegistration in endpoints)
        {
            await DispatchToHandler(endpointRegistration, eventMessage, cancellationToken);
        }
    }
    
    private async ValueTask BroadcastInternalAsync<TEvent>(EndpointRegistration registration, TEvent eventMessage, bool toRemote,
        CancellationToken cancellationToken)
        where TEvent : IEvent
    {
        var endpoints = FilterEndpoints<TEvent>(toRemote, endpointRegistry.GetEndpointsForEvent<TEvent>);

        foreach (var endpointRegistration in endpoints)
        {
            await DispatchToHandler(endpointRegistration, eventMessage, cancellationToken);
        }
    }

    private IEnumerable<EndpointRegistration> FilterEndpoints<TMessage>(bool toRemote,
        Func<Func<EndpointRegistration, bool>, IEnumerable<EndpointRegistration>> getEndpointsFunc)
        where TMessage : IMessage
    {
        var predicate = new Func<EndpointRegistration, bool>(x =>
            (toRemote && !x.Config.IsLocalEndpoint) || (!toRemote && x.Config.IsLocalEndpoint));
        return getEndpointsFunc(predicate);
    }

    private async ValueTask DispatchToHandler(EndpointRegistration endpointRegistration, IMessage message,
        CancellationToken cancellationToken)
    {
        try
        {
            var endpointInstance = serviceProvider.GetRequiredService(endpointRegistration.EndpointType);

            // Check if the message is a command and the handler can handle this command type.
            if (message is ICommand command)
            {
                var handleInterfaceType = typeof(IHandle<>).MakeGenericType(command.GetType());
                if (handleInterfaceType.IsInstanceOfType(endpointInstance))
                {
                    var handleMethod = handleInterfaceType.GetMethod(nameof(IHandle<ICommand>.HandleCommandAsync));
                    if (handleMethod != null)
                    {
                        await (ValueTask)handleMethod.Invoke(endpointInstance,
                            new object[] { command, cancellationToken });
                        return;
                    }
                }
            }

            // Check if the message is an event and the handler can handle this event type.
            if (message is IEvent @event)
            {
                var listenInterfaceType = typeof(IListenFor<>).MakeGenericType(@event.GetType());
                if (listenInterfaceType.IsInstanceOfType(endpointInstance))
                {
                    var handleEventMethod =
                        listenInterfaceType.GetMethod(nameof(IListenFor<IEvent>.HandleEventAsync));
                    if (handleEventMethod != null)
                    {
                        await (ValueTask)handleEventMethod.Invoke(endpointInstance,
                            new object[] { @event, cancellationToken });
                        return;
                    }
                }
            }

            throw new InvalidOperationException(
                $"Endpoint {endpointInstance.GetType().Name} does not implement expected handler interface for message type {message.GetType().Name}.");
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error dispatching message {MessageType} to endpoint {EndpointType}",
                message.GetType().Name, endpointRegistration.EndpointType.Name);
        }
    }
}