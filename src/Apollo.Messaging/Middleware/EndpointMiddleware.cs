using System.Text;
using System.Text.Json;
using Apollo.Messaging.Endpoints;
using Apollo.Nats;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Apollo.Messaging.Middleware;

public class EndpointMiddleware : IMessageMiddleware
{
    private readonly IEndpointRegistry endpointRegistry;
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger<EndpointMiddleware> logger;

    public EndpointMiddleware(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
        endpointRegistry = serviceProvider.GetRequiredService<IEndpointRegistry>();
        logger = serviceProvider.GetRequiredService<ILogger<EndpointMiddleware>>();
        
    }

    public async Task InvokeAsync(NatsMessage message, Func<Task> next, CancellationToken cancellationToken)
    {
        var messageType = message.Message?.GetType();
        if (messageType == null) throw new ArgumentNullException(nameof(messageType));
        
        var endpointRegistrations = 
            endpointRegistry.GetEndpointRegistrations(
                reg => 
                    reg.Subjects.Contains(message.Subject)
                    && reg.HandlerTypes.Any(handlerType => handlerType.GetMessageType() == messageType));

        foreach (var registration in endpointRegistrations)
        {
            var handlerType = registration.HandlerTypes.FirstOrDefault(ht => ht.GetMessageType() == messageType);
            if (handlerType == null)
            {
                logger.LogError("No handler found for message type {MessageType}", messageType);
                continue;
            }; // throw an exception?
            
            var endpoint = serviceProvider.GetRequiredService(registration.EndpointType);
            var handleMethod = handlerType.GetMethod("HandleAsync");
            if (handleMethod == null)
            {
                logger.LogError("No handle method found for message type {MessageType}", messageType);
                continue;
            }

            if (messageType.IsRequest())
            {
                var response = await (dynamic)handleMethod.Invoke(endpoint, [message.Message, cancellationToken])!;
                
                // TODO: still need to configure the serializer
                var json = JsonSerializer.Serialize(response);
                var bytes = Encoding.UTF8.GetBytes(json);
                
                var connection = message.Connection ?? throw new Exception("Connection is null");
                await connection.PublishAsync(message.ReplyTo, bytes, cancellationToken: cancellationToken);
            }
            else
            {
                await (Task)handleMethod.Invoke(endpoint, [message.Message, cancellationToken])!;
            }
        }

        // If no handler was found, continue to the next middleware
        await next();
    }
}