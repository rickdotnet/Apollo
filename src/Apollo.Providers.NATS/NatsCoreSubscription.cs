using Apollo.Abstractions;
using Apollo.Configuration;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;

namespace Apollo.Providers.NATS;

internal class NatsCoreSubscription : ISubscription
{
    private readonly INatsConnection connection;
    private readonly ILogger<NatsCoreSubscription> logger;
    private readonly SubscriptionConfig config;
    private readonly Func<ApolloContext, CancellationToken, Task> handler;
    private readonly string endpointSubject;
    private readonly Dictionary<string, Type> subjectTypeMapping;
    private readonly DefaultSubjectTypeMapper subjectTypeMapper;

    public NatsCoreSubscription(
        INatsConnection connection,
        ILogger<NatsCoreSubscription> logger,
        SubscriptionConfig config,
        Func<ApolloContext, CancellationToken, Task> handler
    )
    {
        this.connection = connection;
        this.logger = logger;
        this.config = config;
        this.handler = handler;

        subjectTypeMapper = DefaultSubjectTypeMapper.From(config);
        endpointSubject = subjectTypeMapper.Subject;
        subjectTypeMapping = subjectTypeMapper.SubjectTypeMapping;
    }

    public async Task Subscribe(CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Subscribing to {Endpoint} - {Subject}", config.EndpointName, endpointSubject);
            await foreach (var msg in connection.SubscribeAsync<byte[]>(endpointSubject)
                               .WithCancellation(cancellationToken))
            {
                var handlerOnly = config.EndpointType == null;
                try
                {
                    // type mapping is for endpoint types only
                    var subjectMapping = "";
                    if (msg.Headers != null && msg.Headers.TryGetValue(ApolloHeader.MessageType, out var apolloType))
                        subjectMapping = apolloType.First() ?? "";

                    if (handlerOnly || subjectTypeMapping.ContainsKey(subjectMapping))
                        await ProcessMessage(msg);
                    else
                        logger.LogWarning(
                            "No handler found for {Subject} in endpoint ({Endpoint})",
                            msg.Subject,
                            config.EndpointName);
                }

                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing message from {Subject}", msg.Subject);
                }
            }
        }
        catch (TaskCanceledException)
        {
            logger.LogWarning("TaskCanceledException");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error subscribing to {EndpointSubject}", endpointSubject);
        }

        return;

        Task ProcessMessage(NatsMsg<byte[]> natsMsg)
        {
            var message = new ApolloMessage
            {
                Subject = natsMsg.Subject,
                Headers = natsMsg.Headers ?? new NatsHeaders(),
                Data = natsMsg.Data,
            };

            if (message.Headers.TryGetValue(ApolloHeader.MessageType, out var headerType)
                && headerType.Count > 0)
            {
                message.MessageType =
                    subjectTypeMapper.TypeFromApolloMessageType(headerType.First()!); // ?? typeof(byte[]);
            }

            var replyFunc = natsMsg.ReplyTo != null
                ? new Func<byte[], CancellationToken, Task>(
                    (response, innerCancel) =>
                        connection.PublishAsync(natsMsg.ReplyTo, response, cancellationToken: innerCancel).AsTask()
                )
                : null;

            return handler(new ApolloContext(message, replyFunc), cancellationToken);
        }
    }
}