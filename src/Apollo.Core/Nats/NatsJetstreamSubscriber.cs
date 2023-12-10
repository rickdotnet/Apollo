using System.Text;
using System.Text.Json;
using Apollo.Core.Endpoints;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;

namespace Apollo.Core.Nats;

internal class NatsJetStreamSubscriber : INatsSubscriber
{
    private readonly INatsConnection connection;
    private readonly NatsSubscriptionConfig config;
    private readonly ILogger<NatsJetStreamSubscriber> logger;
    //private readonly string filterSubject;
    private readonly NatsSubOpts? opts;
    private readonly CancellationToken cancellationToken;

    internal NatsJetStreamSubscriber(
        INatsConnection connection,
        NatsSubscriptionConfig config,
        ILogger<NatsJetStreamSubscriber> logger,
        CancellationToken cancellationToken = default)
    {
        this.connection = connection;
        this.config = config ?? throw new ArgumentNullException(nameof(config));
        //this.filterSubject = config.EndpointSubject;
        this.logger = logger;
        this.cancellationToken = cancellationToken;
    }

    public async Task SubscribeAsync(Func<NatsMessageReceivedEvent, CancellationToken, Task<bool>> handler)
    {
        var js = new NatsJSContext((NatsConnection)connection);

        // namespace without the message type
        var streamNameClean = config.EndpointSubject.Replace(".", "_").Replace("*","").Replace(">","").TrimEnd('_');
        
        // create or get the stream
        //var jsSubject = $"{config.Namespace}.>";
        //var filterSubject = $"{this.filterSubject}.>";

        // TODO: the client doesn't handle errors well here
        //       for instance, a stream can't have overlapping subjects
        //       either we need to fix the client
        //       or validate the stream config is properly set up
        // var test = await js.CreateStreamAsync(new StreamConfig(streamNameClean, new[] { "wtf22.>" }),
        //     cancellationToken);
        
        logger.LogInformation("Creating stream {StreamName} for {Subjects}", streamNameClean, config.EndpointSubject);
        await js.CreateStreamAsync(new StreamConfig(streamNameClean, new[] { config.EndpointSubject }), cancellationToken);
        logger.LogInformation("Stream {StreamName} created for {Subjects}", streamNameClean, config.EndpointSubject);

        logger.LogInformation("Creating consumer {ConsumerName} for stream {StreamName}", config.ConsumerName,
            streamNameClean);

        var consumerConfig = new ConsumerConfig(config.ConsumerName);
        //consumerConfig.FilterSubject = filterSubject.ToLower();
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
        }
    }

    private async Task ProcessMessage(NatsJSMsg<byte[]> msg, Func<NatsMessageReceivedEvent, CancellationToken, Task<bool>> handler)
    {
        var json = Encoding.UTF8.GetString(msg.Data);
        logger.LogInformation("JSON: {Json}", json);

        var type = config.MessageTypes[msg.Subject];
        
        logger.LogInformation("Deserializing message to {TypeName}", type.Name);
        var messageType = type.GetMessageType();
        
        // this will eventually be a configured serializer
        var deserialized = JsonSerializer.Deserialize(json, messageType, new JsonSerializerOptions{ PropertyNameCaseInsensitive = true });
        var message = new NatsMessageReceivedEvent
        {
            Subject = msg.Subject,
            Config = config,
            Headers = msg.Headers,
            Message = deserialized,
            //ReplyTo = msg.ReplyTo, // instead of using these two
            Connection = msg.Connection // we should use a callback instead
        };

        // leaving this point here in case we decide to handle replies here
        // I don't think we will, but, it's here if we need it
        // we might even do some logging based on the result of the handler
        var result = await handler(message, cancellationToken);
    }
}