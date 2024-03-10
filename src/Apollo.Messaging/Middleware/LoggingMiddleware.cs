using Apollo.Nats;
using Microsoft.Extensions.Logging;

namespace Apollo.Messaging.Middleware;

public class LoggingMiddleware : IMessageMiddleware
{
    private readonly ILogger logger;

    public LoggingMiddleware(ILogger<LoggingMiddleware> logger)
    {
        this.logger = logger;
    }

    public async Task InvokeAsync(NatsMessage message, Func<Task> next, CancellationToken cancellationToken)
    {
        var messageTypeName = message.Message?.GetType().Name;
        logger.LogInformation("Processing message of type {MessageType}", messageTypeName);
        
        await next();

        logger.LogInformation("Finished processing message of type {MessageType}", messageTypeName);
    }
}