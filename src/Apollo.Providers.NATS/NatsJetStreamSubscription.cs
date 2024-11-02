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
    private readonly DefaultSubjectTypeMapper subjectTypeMapper;
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

        subjectTypeMapper = DefaultSubjectTypeMapper.From(config);
        endpointSubject = subjectTypeMapper.Subject;
        subjectTypeMapping = subjectTypeMapper.SubjectTypeMapping;
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
                    var subjectMapping = "";
                    if (msg.Headers != null && msg.Headers.TryGetValue(ApolloHeader.MessageType, out var apolloType))
                        subjectMapping = apolloType.First() ?? "";

                    if (handlerOnly || subjectTypeMapping.ContainsKey(subjectMapping))
                    {
                        await ProcessMessage(msg);
                        await msg.AckAsync(cancellationToken: cancellationToken);
                    }
                    else
                    {
                        logger.LogWarning(
                            "No handler found for {Subject} in endpoint ({Endpoint})",
                            subjectMapping,
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
            
            var subjectMapping = "";
            if (natsMsg.Headers != null && natsMsg.Headers.TryGetValue(ApolloHeader.MessageType, out var apolloType))
                subjectMapping = apolloType.First() ?? "";
            
            subjectTypeMapping.TryGetValue(subjectMapping, out var messageType);
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