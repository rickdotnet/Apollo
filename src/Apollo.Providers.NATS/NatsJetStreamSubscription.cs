using Apollo.Abstractions;
using Apollo.Configuration;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;

namespace Apollo.Providers.NATS;

internal class NatsJetStreamSubscription : ISubscription
{
    private readonly INatsConnection connection;
    private readonly ILogger<NatsJetStreamSubscription> logger;
    private readonly SubscriptionConfig config;
    private readonly Func<ApolloContext, CancellationToken, Task> handler;
    private readonly string endpointSubject;
    private readonly Dictionary<string, Type> subjectTypeMapping;

    public NatsJetStreamSubscription(
        INatsConnection connection,
        ILogger<NatsJetStreamSubscription> logger,
        SubscriptionConfig config,
        Func<ApolloContext, CancellationToken, Task> handler
    )
    {
        this.connection = connection;
        this.logger = logger;
        this.config = config;
        this.handler = handler;

        endpointSubject = Utils.GetSubject(config);

        var trimmedSubject = endpointSubject.TrimWildEnds();
        subjectTypeMapping = config.MessageTypes.ToDictionary(x => $"{trimmedSubject}.{x.Name.ToLower()}", x => x);
    }

    public async Task Subscribe(CancellationToken cancellationToken)
    {
        try
        {
            var js = new NatsJSContext((NatsConnection)connection);

            var streamNameClean = endpointSubject.CleanStreamName();

            logger.LogWarning("Create Missing Resources? {CreateMissingResources}", config.CreateMissingResources);
            if (config.CreateMissingResources)
            {
                logger.LogTrace("Creating stream {StreamName} for {Subjects}", streamNameClean,
                    endpointSubject);
                await js.CreateStreamAsync(
                    new StreamConfig(streamNameClean, new[] { endpointSubject }),
                    cancellationToken);
            }

            logger.LogTrace("Creating consumer {ConsumerName} for stream {StreamName}", config.ConsumerName,
                streamNameClean);

            var consumerConfig = new ConsumerConfig(config.ConsumerName);
            var consumer = config.CreateMissingResources
                ? await js.CreateOrUpdateConsumerAsync(streamNameClean, consumerConfig, cancellationToken)
                : await js.GetConsumerAsync(streamNameClean, config.ConsumerName, cancellationToken);

            logger.LogInformation("Subscribing to {Subject}", endpointSubject);
            await foreach (var msg in consumer.ConsumeAsync<byte[]>().WithCancellation(cancellationToken))
            {
                var handlerOnly = config.EndpointType == null;
                try
                {
                    if (handlerOnly || subjectTypeMapping.ContainsKey(msg.Subject))
                    {
                        await ProcessMessage(msg);
                        await msg.AckAsync(cancellationToken: cancellationToken);
                    }
                    else
                    {
                        logger.LogWarning(
                            "No handler found for {Subject} in endpoint ({Endpoint})",
                            msg.Subject,
                            config.EndpointName);

                        await msg.AckTerminateAsync(cancellationToken: cancellationToken);
                    }
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

        Task ProcessMessage(NatsJSMsg<byte[]> natsMsg)
        {
            var message = new ApolloMessage
            {
                Subject = natsMsg.Subject,
                Headers = natsMsg.Headers ?? new NatsHeaders(),
                Data = natsMsg.Data,
            };

            subjectTypeMapping.TryGetValue(message.Subject, out var messageType);
            message.MessageType = messageType ?? typeof(byte[]);

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