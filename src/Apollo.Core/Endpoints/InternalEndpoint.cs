using System.Text;
using System.Text.Json;
using Apollo.Core.Messaging;
using Apollo.Core.Messaging.Events;
using Apollo.Core.Nats;
using Microsoft.Extensions.Logging;

namespace Apollo.Core.Endpoints;

internal class InternalEndpoint(
    IApolloDispatcher dispatcher,
    IEndpointRegistry endpointRegistry,
    ILogger<InternalEndpoint> logger
    )
    : IListenFor<NatsMessageReceivedEvent>
{
    public async ValueTask HandleEventAsync(NatsMessageReceivedEvent wrapper, CancellationToken cancellationToken = default)
    {
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