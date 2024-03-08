using System.Text;
using System.Text.Json;
using Apollo.Abstractions.Messaging.Events;
using Apollo.Endpoints;
using Apollo.Nats;
using Microsoft.Extensions.Logging;

namespace Apollo.Messaging.Endpoints;

internal class InternalEndpoint : IListenFor<NatsMessageReceivedEvent>
{
    private readonly IApolloDispatcher dispatcher;
    private readonly IEndpointRegistry endpointRegistry;
    private readonly ILogger<InternalEndpoint> logger;

    public InternalEndpoint(IApolloDispatcher dispatcher,
        IEndpointRegistry endpointRegistry,
        ILogger<InternalEndpoint> logger)
    {
        this.dispatcher = dispatcher;
        this.endpointRegistry = endpointRegistry;
        this.logger = logger;
    }

    public async Task HandleEventAsync(NatsMessageReceivedEvent wrapper, CancellationToken cancellationToken = default)
    {
        if (wrapper.Message is null)
        {
            logger.LogWarning("Discarding null message from {Subject}", wrapper.Subject);
            return;
        }

        var messageType = wrapper.Message.GetType();

        logger.LogInformation("InternalEndpoint Received: {MessageType} from {Subject}", messageType.Name, wrapper.Subject);
        var registrations = endpointRegistry.GetEndpointRegistrations()
            .Where(x=>x.Subjects.Contains(wrapper.Subject));

        foreach (var registration in registrations)
        {
            foreach (var handler in registration.HandlerTypes.Where(x => x.GetMessageType() == messageType))
            {
                // messages can act as commands or events
                // so we need to check both
                // ultimately we could disallow this, but
                // until then we need to let it flow through
                // both checks instead of using 'else if'
                if (handler.IsListener())
                    await dispatcher.BroadcastToSingleRemoteEndpointAsync(registration, messageType, wrapper.Message, cancellationToken);
                if (handler.IsCommandHandler())
                    await dispatcher.SendCommandToSingleRemoteEndpointsAsync(registration, messageType, wrapper.Message, cancellationToken);
                if (handler.IsRequestHandler())
                {
                    var response = await dispatcher.SendRequestToRemoteEndpointsAsync(messageType, wrapper.Message, cancellationToken);

                    var json = JsonSerializer.Serialize(response);
                    var bytes = Encoding.UTF8.GetBytes(json);
                
                    // I don't like this, but this gets it working
                    // we'll eventually use a callback and reply in
                    // in the subscriber instead of here
                    await wrapper.Connection.PublishAsync(wrapper.ReplyTo, bytes, cancellationToken: cancellationToken);
                }
            
                // could throw an exception here if we don't find a handler
            }
        }
    }
}