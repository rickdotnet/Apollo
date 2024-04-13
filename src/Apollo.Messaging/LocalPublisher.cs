using Apollo.Messaging.Abstractions;
using IdGen;
using Microsoft.Extensions.Logging;

namespace Apollo.Messaging;

internal class LocalPublisher : IPublisher
{
    public string Route { get; }

    public bool IsLocalOnly => true;

    private readonly MessageProcessor messageProcessor;
    private readonly IdGenerator idGenerator;
    private readonly ILogger<LocalPublisher> logger;

    public LocalPublisher(string route, MessageProcessor messageProcessor, IdGenerator idGenerator, ILogger<LocalPublisher> logger)
    {
        Route = route;
        this.messageProcessor = messageProcessor;
        this.idGenerator = idGenerator;
        this.logger = logger;
    }


    public async Task SendCommandAsync<TCommand>(TCommand commandMessage, CancellationToken cancellationToken)
        where TCommand : ICommand
    {
        var message = new MessageContext
        {
            Subject = $"{Route}.{typeof(TCommand).Name}".ToLower(),
            Message = commandMessage,
            Source = "local",
            Headers = new Dictionary<string, string>
            {
                { "Message-Id", idGenerator.CreateId().ToString() }
            }
        };
        var result = await messageProcessor.ProcessLocalMessageAsync(message, cancellationToken);
        logger.LogDebug("Command {Name} processed with result {Result}", typeof(TCommand).Name, result);
    }

    public async Task BroadcastAsync<TEvent>(TEvent eventMessage, CancellationToken cancellationToken)
        where TEvent : IEvent
    {
        var message = new MessageContext
        {
            Subject = $"{Route}.{typeof(TEvent).Name}".ToLower(),
            Message = eventMessage,
            Source = "local",
            Headers = new Dictionary<string, string>
            {
                { "Message-Id", idGenerator.CreateId().ToString() }
            }
        };

        var result = await messageProcessor.ProcessLocalMessageAsync(message, cancellationToken);
        logger.LogDebug("Event {Name} broadcasted", typeof(TEvent).Name);
    }

    public async Task<TResponse?> SendRequestAsync<TRequest, TResponse>(TRequest requestMessage,
        CancellationToken cancellationToken) where TRequest : IRequest<TResponse>
    {
        var message = new MessageContext
        {
            Subject = $"{Route}.{typeof(TRequest).Name}".ToLower(),
            Message = requestMessage,
            Source = "local",
            Headers = new Dictionary<string, string>
            {
                { "Message-Id", idGenerator.CreateId().ToString() }
            }
        };

        var result = await messageProcessor.ProcessLocalMessageAsync(message, cancellationToken);
        logger.LogDebug("Request {Name} processed with result {Result}", typeof(TRequest).Name, result);

        var response = (TResponse?)result;
        return response;
    }

    public Task SendObjectAsync(string subject, object message, CancellationToken cancellationToken)
    {
        var msgId = idGenerator.CreateId().ToString();
        var messageContext = new MessageContext
        {
            Subject = subject,
            Message = message,
            Source = "local",
            Headers = new Dictionary<string, string>
            {
                { "Message-Id", msgId },
                { "Nats-Msg-Id", msgId } // do we want to set this?
            }
        };
        
        return messageProcessor.ProcessLocalMessageAsync(messageContext, cancellationToken);
    }

    public Task<object?> SendRequestAsync(string subject, object requestMessage, CancellationToken cancellationToken)
    {
        var messageContext = new MessageContext
        {
            Subject = subject,
            Message = requestMessage,
            Source = "local",
            Headers = new Dictionary<string, string>
            {
                { "Message-Id", idGenerator.CreateId().ToString() }
            }
        };
        
        return messageProcessor.ProcessLocalMessageAsync(messageContext, cancellationToken);
    }
}