using System.Text;
using System.Text.Json;
using Apollo.Configuration;
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
        try
        {
            logger.LogInformation("Subscribing to {Subject}", config.EndpointSubject);
            await foreach (var msg in connection.SubscribeAsync<byte[]>(config.EndpointSubject)
                               .WithCancellation(cancellationToken))
            {
                try
                {
                    logger.LogInformation("Subscriber received message from {Subject}", msg.Subject);
                    if (config.MessageTypes.ContainsKey(msg.Subject))
                        await ProcessMessage(msg);
                    else
                        logger.LogWarning("No handler found for {Subject} in endpoint ({Endpoint})", msg.Subject,
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
            logger.LogError(ex, "Error subscribing to {EndpointSubject}", config.EndpointSubject);
        }

        return;

        async Task ProcessMessage(NatsMsg<byte[]> natsMsg)
        {
            var message = new ApolloMessage
            {
                Subject = natsMsg.Subject,
                Headers = natsMsg.Headers,
                ReplyTo = natsMsg.ReplyTo
            };

            if (natsMsg.Data != null)
            {

                var json = Encoding.UTF8.GetString(natsMsg.Data);
                logger.LogTrace("JSON: {Json}", json);

                var type = config.MessageTypes[natsMsg.Subject].GetMessageType();
                logger.LogInformation("Deserializing message to {TypeName}", type.Name);

                // TODO: figure out serializer
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