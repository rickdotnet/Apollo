using Apollo.Messaging.Endpoints;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Apollo.Messaging.Middleware;

public class EndpointMiddleware : IMessageMiddleware
{
    private readonly IEnumerable<IEndpointRegistry> endpointRegistries;
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger<EndpointMiddleware> logger;

    public EndpointMiddleware(IEnumerable<IEndpointRegistry> endpointRegistries, IServiceProvider serviceProvider, ILogger<EndpointMiddleware> logger)
    {
        this.endpointRegistries = endpointRegistries;
        this.serviceProvider = serviceProvider;
        this.logger = logger;
    }

    public async Task InvokeAsync(MessageContext messageContext, Func<Task> next, CancellationToken cancellationToken)
    {
        var messageType = messageContext.Message?.GetType();
        ArgumentNullException.ThrowIfNull(messageType, nameof(messageType));
        
        var endpointRegistrations = 
            endpointRegistries.SelectMany(x=>x.GetEndpointRegistrations(
                reg => reg.SubjectMapping.ContainsKey(messageContext.Subject)));
            

        foreach (var registration in endpointRegistrations)
        {
            var handlerType = registration.HandlerTypes.FirstOrDefault(ht => ht.GetMessageType() == messageType);
            if (handlerType == null)
            {
                logger.LogError("No handler found for message type {MessageType}", messageType);
                continue;
            }; // throw an exception?
            
            var endpoint = serviceProvider.GetRequiredService(registration.EndpointType);
            if(endpoint is EndpointBase baseEndpoint)
                baseEndpoint.SetContext(messageContext);
            
            var handleMethod = handlerType.GetMethod("HandleAsync");
            if (handleMethod == null)
            {
                logger.LogError("No handle method found for message type {MessageType}", messageType);
                continue;
            }

            if (messageType.IsRequest())
            {
                var response = await (dynamic)handleMethod.Invoke(endpoint, [messageContext.Message, cancellationToken])!;
                _ = response ?? throw new InvalidOperationException("Response is null");
                
                await messageContext.ReplyAsync((object)response, cancellationToken);
            }
            else
            {
                await (Task)handleMethod.Invoke(endpoint, [messageContext.Message, cancellationToken])!;
            }
        }

        // continue to the next middleware
        await next();
    }
}