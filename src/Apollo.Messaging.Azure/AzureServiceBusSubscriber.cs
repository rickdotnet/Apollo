using System.Text.Json;
using Apollo.Configuration;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.Logging;

namespace Apollo.Messaging.Azure;

public class AzureServiceBusSubscriber : ISubscriber
{
    private readonly ApolloConfig apolloConfig;
    private readonly ServiceBusClient client;
    private readonly ILogger logger;
    private readonly BusResourceManager resourceManager;

    public AzureServiceBusSubscriber(
        ApolloConfig config,
        ServiceBusClient client,
        ILogger<AzureServiceBusSubscriber> logger,
        BusResourceManager resourceManager)
    {
        this.apolloConfig = config;
        this.client = client;
        this.logger = logger;
        this.resourceManager = resourceManager;
    }

    public async Task SubscribeAsync(SubscriptionConfig config, Func<ApolloMessage, CancellationToken, Task> handler,
        CancellationToken cancellationToken)
    {
        try
        {
            // this was built for NATS so we need to strip the wildcard
            var topicName = config.EndpointSubject.Replace(".>", "").Replace(".*", "");
            logger.LogWarning("Subscribing to {EndpointSubject}", topicName);
            logger.LogTrace("CreateMissingResources: {CreateMissingResources}", config.CreateMissingResources);

            // precious real estate means abbreviations
            var durableSuffix = config.IsDurableConsumer ? "d" : "nd";
            var subscriptionSuffix = $".{topicName}.{durableSuffix}".ToLower();

            // ASB only lets the subscription name be 50 characters
            var safeConsumerLength = 50 - subscriptionSuffix.Length;
            var safeConsumerName = config.ConsumerName.Length > safeConsumerLength
                ? config.ConsumerName[..safeConsumerLength]
                : config.ConsumerName;

            var safeSubscriptionName = $"{safeConsumerName}{subscriptionSuffix}".ToLower();
            var subscriptionOptions = new CreateSubscriptionOptions(topicName, safeSubscriptionName)
            {
                // the original subscription name for full reference
                UserMetadata = $"{config.ConsumerName}{subscriptionSuffix}"
            };

            if (!config.IsDurableConsumer)
            {
                subscriptionOptions.DefaultMessageTimeToLive = TimeSpan.FromMinutes(1);
                subscriptionOptions.AutoDeleteOnIdle = TimeSpan.FromMinutes(5);
            }

            var topicExists = await resourceManager.TopicExistsAsync(subscriptionOptions.TopicName, cancellationToken);
            if (!topicExists)
            {
                if (!config.CreateMissingResources) throw new InvalidOperationException($"Missing topic: {subscriptionOptions.TopicName}");
                logger.LogTrace("Creating topic {TopicName}", subscriptionOptions.TopicName);
                await resourceManager.CreateTopicAsync(subscriptionOptions.TopicName, cancellationToken);
            }

            var subscriptionExists = await resourceManager.SubscriptionExistsAsync(subscriptionOptions.TopicName,
                subscriptionOptions.SubscriptionName, cancellationToken);
            if (!subscriptionExists)
            {
                if (!config.CreateMissingResources) 
                    throw new InvalidOperationException($"Missing subscription ({subscriptionOptions.SubscriptionName}) on topic ({subscriptionOptions.SubscriptionName})");
                
                logger.LogTrace("Creating subscription {SubscriptionName} on topic {TopicName}",
                    subscriptionOptions.SubscriptionName, subscriptionOptions.TopicName);
                await resourceManager.CreateSubscriptionAsync(subscriptionOptions, cancellationToken);
            }

            var identifier = config.InstanceId.ToString();
            await using var processor = client.CreateProcessor(
                subscriptionOptions.TopicName,
                subscriptionOptions.SubscriptionName,
                new ServiceBusProcessorOptions
                {
                    AutoCompleteMessages = false,
                    //ReceiveMode = receiveMode,
                    Identifier = identifier,
                }
            );

            // configure the message and error handler to use
            processor.ProcessMessageAsync += ProcessMessage;
            processor.ProcessErrorAsync += ErrorHandler;

            await processor.StartProcessingAsync(cancellationToken);
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        catch (TaskCanceledException)
        {
            logger.LogWarning("MessageProcessor TaskCanceledException");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error subscribing to {EndpointSubject}", config.EndpointSubject);
        }

        return;

        Task ErrorHandler(ProcessErrorEventArgs arg)
        {
            logger.LogError(arg.Exception, "Error processing message");
            return Task.CompletedTask;
        }

        async Task ProcessMessage(ProcessMessageEventArgs args)
        {
            try
            {
                logger.LogTrace("Processing message {MessageId}", args.Message.MessageId);
                await ActuallyProcessMessage(args.Message);
                logger.LogTrace("Completed message {MessageId}", args.Message.MessageId);
                
                await args.CompleteMessageAsync(args.Message, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing message {MessageId}", args.Message.MessageId);
                await args.AbandonMessageAsync(args.Message, cancellationToken: cancellationToken);
                throw; // need to make sure this doesn't crash the processor
            }
        }

        Task ActuallyProcessMessage(ServiceBusReceivedMessage msg)
        {
            var messageType = msg.ApplicationProperties["message-type"].ToString();
            var message = new ApolloMessage
            {
                Subject = $"{msg.Subject}.{messageType}",
                //Headers = msg.ApplicationProperties,
                ReplyTo = msg.ReplyTo,
            };

            if (msg.Body != null)
            {
                var json = msg.Body.ToString();
                logger.LogTrace("JSON:\n{Json}", json);

                var type = config.GetMessageType(message.Subject);
                if (type == null)
                {
                    // if ThrowOnMissingMessageType or something like that
                    // throw new InvalidOperationException($"No message type found for {message.Subject}");
                    logger.LogWarning("No message type found for {Subject}", message.Subject);
                    logger.LogWarning("Defaulting to object");
                    type = typeof(object); // not sure if this is the best idea
                }

                logger.LogTrace("Deserializing message to {TypeName}", type.Name);

                // TODO: figure out serializer
                var deserialized = JsonSerializer.Deserialize(
                    json, type, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                message.Message = deserialized;
            }

            if (message.ReplyTo != null)
                message.Replier = new AzureReplier(client, $"{msg.ReplyTo}.{msg.ReplyToSessionId}");

            return handler(message, cancellationToken);
        }
    }
}