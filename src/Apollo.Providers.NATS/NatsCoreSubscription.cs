using Apollo.Abstractions;
using Apollo.Configuration;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;

namespace Apollo.Providers.NATS;

internal class NatsCoreSubscription : ISubscription
{
    private readonly NatsConnection connection;
    private readonly ILogger<NatsCoreSubscription> logger;
    private readonly SubscriptionConfig config;
    private readonly Func<ApolloContext, CancellationToken, Task> handler;
    private readonly string endpointSubject;
    private readonly Dictionary<string, Type> subjectTypeMapping;

    public NatsCoreSubscription(
        NatsConnection connection,
        ILogger<NatsCoreSubscription> logger,
        SubscriptionConfig config,
        Func<ApolloContext, CancellationToken, Task> handler
    )
    {
        this.connection = connection;
        this.logger = logger;
        this.config = config;
        this.handler = handler;

        endpointSubject = Utils.GetSubject(config);

        var trimmedSubject = endpointSubject.TrimEnd('*').TrimEnd('>').TrimEnd('.');
        subjectTypeMapping = config.MessageTypes.ToDictionary(x => $"{trimmedSubject}.{x.Name.ToLower()}", x => x);
    }

    public async Task SubscribeAsync(CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Subscribing to {Subject}", endpointSubject);
            await foreach (var msg in connection.SubscribeAsync<byte[]>(endpointSubject)
                               .WithCancellation(cancellationToken))
            {
                var handlerOnly = config.EndpointType == null;
                try
                {
                    if (handlerOnly || subjectTypeMapping.ContainsKey(msg.Subject))
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
                Headers = natsMsg.Headers,
                Data = natsMsg.Data,
            };

            subjectTypeMapping.TryGetValue(message.Subject, out var messageType);
            message.MessageType = messageType; // ?? typeof(byte[]);

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