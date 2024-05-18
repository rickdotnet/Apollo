using Apollo.Messaging.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Apollo.Messaging.Publishing;

internal class LocalPublisherFactory: ILocalPublisherFactory
{
    private readonly MessageProcessor messageProcessor;

    private readonly ILogger<LocalPublisher> logger;

    public LocalPublisherFactory(MessageProcessor messageProcessor, ILogger<LocalPublisher>? logger)
    {
        this.messageProcessor = messageProcessor;
        this.logger = logger ?? NullLogger<LocalPublisher>.Instance;
    }

    public ILocalPublisher CreatePublisher(string route)
    {
        return new LocalPublisher(route, messageProcessor, logger);
    }
}