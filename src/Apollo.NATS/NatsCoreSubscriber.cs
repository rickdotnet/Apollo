using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;

namespace Apollo.NATS;

public class NatsCoreSubscriber : ISubscriber
{
    private readonly INatsConnection connection;
    private readonly SubscriptionConfig config;
    private readonly ILogger logger;
    private readonly string subject;
    private readonly string? queueGroup;
    private readonly ISerializeThings? serializer;
    private readonly NatsSubOpts? opts;
    private readonly CancellationToken cancellationToken;


    public NatsCoreSubscriber(
        INatsConnection connection,
        SubscriptionConfig config,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        this.connection = connection;
        this.config = config;
        this.logger = logger;
        this.subject = config.EndpointSubject;
        this.queueGroup = config.ConsumerName;
        this.serializer = config.Serializer;
        this.opts = config.NatsSubOpts;
        this.cancellationToken = cancellationToken;
    }

    public async Task SubscribeAsync(Func<ApolloMessage, CancellationToken, Task> handler)
    {
        logger.LogInformation("Subscribing to {Subject}", subject);
        
        await foreach (var msg in connection.SubscribeAsync<byte[]>(subject, cancellationToken: cancellationToken))
        {
            logger.LogInformation("Subscriber received message from {Subject}", msg.Subject);

            try
            {
                var json = Encoding.UTF8.GetString(msg.Data);
                logger.LogInformation("JSON: {Json}", json);

                var type = config.MessageTypes[msg.Subject].GetMessageType();
                logger.LogInformation("Deserializing message to {TypeName}", type.Name);

                // TODO: figure out serializer
                var deserialized = JsonSerializer.Deserialize(json, type,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                var message = new ApolloMessage
                {
                    Subject = msg.Subject,
                    Headers = msg.Headers,
                    Message = deserialized,
                    ReplyTo = msg.ReplyTo, // instead of using these two
                };

                await handler(message, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing message from {Subject}", msg.Subject);
            }
        }
    }
}