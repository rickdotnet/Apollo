using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;

namespace Apollo.Nats;

public class NatsJetStreamSubscriber : INatsSubscriber
{
    private readonly INatsConnection connection;
    private readonly NatsSubscriptionConfig config;
    private readonly ILogger logger;
    private readonly NatsSubOpts? opts;
    private readonly CancellationToken cancellationToken;

    public NatsJetStreamSubscriber(
        INatsConnection connection,
        NatsSubscriptionConfig config,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        this.connection = connection;
        this.config = config ?? throw new ArgumentNullException(nameof(config));
        //this.filterSubject = config.EndpointSubject;
        this.logger = logger;
        this.cancellationToken = cancellationToken;
    }

    public async Task SubscribeAsync(Func<NatsMessage, CancellationToken, Task> handler)
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
            logger.LogInformation("Subscriber received message from {Subject}", msg.Subject);
            // Process message

            if (config.MessageTypes.ContainsKey(msg.Subject))
            {
                await ProcessMessage(msg, handler);
                await msg.AckAsync(cancellationToken: cancellationToken);
            }
            else
            {
                logger.LogWarning("No handler found for {Subject} in endpoint ({Endpoint})", msg.Subject, config.EndpointName);
                
                // TODO: need to makes sure this doesn't stop the server from
                //       redelivering the message to other processors outside
                //       of this application
                await msg.AckTerminateAsync(cancellationToken: cancellationToken);
            }
        }
    }

    private async Task ProcessMessage(NatsJSMsg<byte[]> msg,
        Func<NatsMessage, CancellationToken, Task> handler)
    {
        var json = Encoding.UTF8.GetString(msg.Data);
        logger.LogInformation("JSON: {Json}", json);

        var type = config.MessageTypes[msg.Subject].GetMessageType();

        logger.LogInformation("Deserializing message to {TypeName}", type.Name);
        // this will eventually be a configured serializer
        var deserialized = JsonSerializer.Deserialize(json, type,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        var message = new NatsMessage
        {
            Subject = msg.Subject,
            Config = config,
            Headers = msg.Headers,
            Message = deserialized,
            ReplyTo = msg.ReplyTo
        };

        await handler(message, cancellationToken);
    }
}