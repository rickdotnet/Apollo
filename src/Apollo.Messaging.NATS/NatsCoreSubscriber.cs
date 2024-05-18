using System.Text;
using System.Text.Json;
using Apollo.Configuration;
using Apollo.Messaging.Abstractions;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;

namespace Apollo.Messaging.NATS;

internal class NatsCoreSubscriber : ISubscriber
{
    private readonly INatsConnection connection;
    private readonly ILogger logger;


    public NatsCoreSubscriber(
        INatsConnection connection,
        ILogger logger
    )
    {
        this.connection = connection;
        this.logger = logger;
    }

    public async Task SubscribeAsync(SubscriptionConfig config, Func<ApolloMessage, CancellationToken, Task> handler,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Subscribing to {Subject}", config.EndpointSubject);

        await foreach (var msg in connection.SubscribeAsync<byte[]>(config.EndpointSubject,
                           cancellationToken: cancellationToken))
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

                if (message.ReplyTo != null)
                    message.Replier = new NatsReplier(connection, message.ReplyTo);

                await handler(message, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing message from {Subject}", msg.Subject);
            }
        }
    }
}