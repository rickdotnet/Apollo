using System.Text.Json;
using Apollo.Abstractions;
using Apollo.Configuration;

namespace Apollo.Internal;

internal class DefaultPublisher : IPublisher
{
    private readonly IProviderPublisher providerPublisher;
    private readonly PublishConfig publishConfig;

    public DefaultPublisher(PublishConfig publishConfig)
    {
        this.publishConfig = publishConfig;
        this.providerPublisher = publishConfig.ProviderPublisher!;
    }

    public Task Send<TCommand>(TCommand commandMessage, CancellationToken cancellationToken) where TCommand : ICommand
    {
        var apolloMessage = new ApolloMessage
        {
            Data = JsonSerializer.SerializeToUtf8Bytes(commandMessage),
            MessageType = typeof(TCommand),
        };

        return providerPublisher.Publish(publishConfig, apolloMessage, cancellationToken);
    }

    public Task Broadcast<TEvent>(TEvent eventMessage, CancellationToken cancellationToken) where TEvent : IEvent
    {
        var apolloMessage = new ApolloMessage
        {
            Data = JsonSerializer.SerializeToUtf8Bytes(eventMessage),
            MessageType = typeof(TEvent),
        };

        return providerPublisher.Publish(publishConfig, apolloMessage, cancellationToken);
    }

    public async Task<TResponse?> Request<TRequest, TResponse>(TRequest requestMessage, CancellationToken cancellationToken) where TRequest : IRequest<TResponse>
    {
        var apolloMessage = new ApolloMessage
        {
            Data = JsonSerializer.SerializeToUtf8Bytes(requestMessage),
            MessageType = typeof(TRequest),
        };

        var response = await providerPublisher.Request(publishConfig, apolloMessage, cancellationToken);

        // yolo?
        return JsonSerializer.Deserialize<TResponse>(response);
    }
}