﻿using Microsoft.Extensions.Logging;

namespace Apollo.Messaging.Middleware;

public class LoggingMiddleware : IMessageMiddleware
{
    private readonly ILogger logger;

    public LoggingMiddleware(ILogger<LoggingMiddleware> logger)
    {
        this.logger = logger;
    }

    public async Task InvokeAsync(MessageContext messageContext, Func<Task> next, CancellationToken cancellationToken)
    {
        var messageTypeName = messageContext.Message?.GetType().Name;
        logger.LogTrace("Processing message of type {MessageType}", messageTypeName);
        
        await next();

        logger.LogTrace("Finished processing message of type {MessageType}", messageTypeName);
    }
}