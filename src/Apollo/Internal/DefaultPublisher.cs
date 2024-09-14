using System.Text.Json;
using Apollo.Abstractions;
using Apollo.Configuration;
using Apollo.Providers;
using Microsoft.Extensions.Primitives;

namespace Apollo.Internal;

internal class DefaultPublisher : IPublisher
{
    private readonly IProviderPublisher providerPublisher;
    private readonly PublishConfig publishConfig;
    private DefaultSubjectTypeMapper subjectTypeMapper;

    public DefaultPublisher(PublishConfig publishConfig)
    {
        this.publishConfig = publishConfig ?? throw new ArgumentNullException(nameof(publishConfig));
        providerPublisher = publishConfig.ProviderPublisher ?? throw new InvalidOperationException("ProviderPublisher cannot be null.");
        
        subjectTypeMapper = DefaultSubjectTypeMapper.From(publishConfig);
    }

    public Task Send<TCommand>(TCommand commandMessage, CancellationToken cancellationToken) where TCommand : ICommand 
        => PublishInternal(commandMessage, "Send", cancellationToken);

    public Task Broadcast<TEvent>(TEvent eventMessage, CancellationToken cancellationToken) where TEvent : IEvent 
        => PublishInternal(eventMessage, "Broadcast", cancellationToken);

    public async Task<TResponse?> Request<TRequest, TResponse>(TRequest requestMessage, CancellationToken cancellationToken) where TRequest : IRequest<TResponse>
    {
        var apolloMessage = CreateApolloMessage(requestMessage, "Request");
        apolloMessage.Headers.Add(ApolloHeader.ResponseType, subjectTypeMapper.ApolloMessageType(typeof(TResponse).Name));
        apolloMessage.Headers.Add(ApolloHeader.ResponseClrType, typeof(TResponse).AssemblyQualifiedName!);

        var response = await providerPublisher.Request(publishConfig, apolloMessage, cancellationToken);
        return JsonSerializer.Deserialize<TResponse>(response);
    }

    private Task PublishInternal<TMessage>(TMessage message, string action, CancellationToken cancellationToken)
    {
        var apolloMessage = CreateApolloMessage(message, action);
        return providerPublisher.Publish(publishConfig, apolloMessage, cancellationToken);
    }

    private ApolloMessage CreateApolloMessage<TMessage>(TMessage message, string action)
    {
        var messageType = typeof(TMessage);
        return new ApolloMessage
        {
            Data = JsonSerializer.SerializeToUtf8Bytes(message),
            MessageType = messageType,
            Headers = new Dictionary<string, StringValues>
            {
                {ApolloHeader.MessageType, subjectTypeMapper.ApolloMessageType(messageType.Name)},
                {ApolloHeader.MessageClrType, messageType.AssemblyQualifiedName!},
                {ApolloHeader.MessageAction, action}
            }
        };
    }
}