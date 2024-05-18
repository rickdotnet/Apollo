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
        var js = new NatsJSContext((NatsConnection)connection);

        // namespace without the message type
        var streamNameClean =
            config
                .EndpointSubject
                .Replace(".", "_")
                .Replace("*", "")
                .Replace(">", "")
                .TrimEnd('_');

        logger.LogInformation("Creating stream {StreamName} for {Subjects}", streamNameClean, config.EndpointSubject);
        await js.CreateStreamAsync(
            new StreamConfig(streamNameClean, new[] { config.EndpointSubject }),
            cancellationToken);

        logger.LogInformation("Stream {StreamName} created for {Subjects}", streamNameClean, config.EndpointSubject);
        logger.LogInformation("Creating consumer {ConsumerName} for stream {StreamName}", config.ConsumerName,
            streamNameClean);

        var consumerConfig = new ConsumerConfig(config.ConsumerName);
        var consumer = await js.CreateOrUpdateConsumerAsync(streamNameClean, consumerConfig, cancellationToken);
        logger.LogInformation("Consumer {ConsumerName} for stream {StreamName} created", config.ConsumerName,
            streamNameClean);

        await foreach (var msg in consumer.ConsumeAsync<byte[]>().WithCancellation(cancellationToken))
        {
            try
            {
                logger.LogInformation("Subscriber received message from {Subject}", msg.Subject);
                // Process message

                if (config.MessageTypes.ContainsKey(msg.Subject))
                {
                    await ProcessMessage(msg, handler);
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

        return;

        async Task ProcessMessage(NatsJSMsg<byte[]> msg,
            Func<ApolloMessage, CancellationToken, Task> messageHandler)
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
                logger.LogInformation("JSON: {Json}", json);

                var type = config.MessageTypes[msg.Subject].GetMessageType();

                logger.LogInformation("Deserializing message to {TypeName}", type.Name);
                
                // this will eventually be a configured serializer
                var deserialized = JsonSerializer.Deserialize(json, type,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                message.Message = deserialized;
            }
            
            if (message.ReplyTo != null)
                message.Replier = new NatsReplier(connection, message.ReplyTo);

            await messageHandler(message, cancellationToken);
        }
    }
}