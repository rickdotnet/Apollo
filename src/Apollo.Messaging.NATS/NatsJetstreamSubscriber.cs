using System.Text;
using System.Text.Json;
using Apollo.Configuration;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;

namespace Apollo.Messaging.NATS;

internal class NatsJetStreamSubscriber : ISubscriber
{
    private readonly INatsConnection connection;
    private readonly ILogger logger;

    public NatsJetStreamSubscriber(
        INatsConnection connection,
        ILogger logger)
    {
        this.connection = connection;
        this.logger = logger;
    }

    public async Task SubscribeAsync(SubscriptionConfig config, Func<ApolloMessage, CancellationToken, Task> handler,
        CancellationToken cancellationToken)
    {
        try
        {
            var js = new NatsJSContext((NatsConnection)connection);

            // namespace without the message type
            var streamNameClean =
                config
                    .EndpointSubject
                    .Replace(".", "_")
                    .Replace("*", "")
                    .Replace(">", "")
                    .TrimEnd('_');


            logger.LogWarning("Create Missing Resources? {CreateMissingResources}", config.CreateMissingResources);
            // TODO: ^ now we need to honor it

            logger.LogTrace("Creating stream {StreamName} for {Subjects}", streamNameClean,
                config.EndpointSubject);
            await js.CreateStreamAsync(
                new StreamConfig(streamNameClean, new[] { config.EndpointSubject }),
                cancellationToken);

            logger.LogTrace("Creating consumer {ConsumerName} for stream {StreamName}", config.ConsumerName,
                streamNameClean);

            var consumerConfig = new ConsumerConfig(config.ConsumerName);
            var consumer = await js.CreateOrUpdateConsumerAsync(streamNameClean, consumerConfig, cancellationToken);

            logger.LogInformation("Subscribing to {Subject}", config.EndpointSubject);
            await foreach (var msg in consumer.ConsumeAsync<byte[]>().WithCancellation(cancellationToken))
            {
                try
                {
                    logger.LogTrace("Subscriber received message from {Subject}", msg.Subject);
                    if (config.MessageTypes.ContainsKey(msg.Subject))
                    {
                        await ProcessMessage(msg);
                        await msg.AckAsync(cancellationToken: cancellationToken);
                    }
                    else
                    {
                        logger.LogWarning("No handler found for {Subject} in endpoint ({Endpoint})", msg.Subject,
                            config.EndpointName);

                        // TODO: need to makes sure this doesn't stop the server from
                        //       redelivering the message to other processors outside
                        //       of this application
                        await msg.AckTerminateAsync(cancellationToken: cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error processing message from {Subject}", msg.Subject);
                    await msg.AckTerminateAsync(cancellationToken: cancellationToken);
                }
            }
        }
        catch (TaskCanceledException)
        {
            logger.LogWarning("TaskCanceledException");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error subscribing to {EndpointSubject}", config.EndpointSubject);
        }

        return;

        async Task ProcessMessage(NatsJSMsg<byte[]> msg)
        {
            var message = new ApolloMessage
            {
                Subject = msg.Subject,
                Headers = msg.Headers,
                ReplyTo = msg.ReplyTo
            };

            if (msg.Data != null)
            {
                var json = Encoding.UTF8.GetString(msg.Data);
                logger.LogTrace("JSON: {Json}", json);

                var type = config.MessageTypes[msg.Subject].GetMessageType();

                logger.LogTrace("Deserializing message to {TypeName}", type.Name);

                // this will eventually be a configured serializer
                var deserialized = JsonSerializer.Deserialize(json, type,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                message.Message = deserialized;
            }

            if (message.ReplyTo != null)
                message.Replier = new NatsReplier(connection, message.ReplyTo);

            await handler(message, cancellationToken);
        }
    }
}