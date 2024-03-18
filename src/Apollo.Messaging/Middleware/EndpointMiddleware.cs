using Apollo.Messaging.Endpoints;
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

    public async Task InvokeAsync(MessageContext messageContext, Func<Task> next, CancellationToken cancellationToken)
    {
        var messageType = messageContext.Message?.GetType();
        if (messageType == null) throw new ArgumentNullException(nameof(messageType));
        
        var endpointRegistrations = 
            endpointRegistry.GetEndpointRegistrations(
                reg => 
                    reg.Subjects.Contains(messageContext.Subject)
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
                
                var response = await (dynamic)handleMethod.Invoke(endpoint, [messageContext.Message, cancellationToken])!;
                var replier = messageContext.Replier ?? throw new Exception("Replier is null");
                _ = response ?? throw new InvalidOperationException("Response is null");
                
                await replier.ReplyAsync((object)response, cancellationToken);
            }
            else
            {
                await (Task)handleMethod.Invoke(endpoint, [messageContext.Message, cancellationToken])!;
            }
        }

        // If no handler was found, continue to the next middleware
        await next();
    }
}