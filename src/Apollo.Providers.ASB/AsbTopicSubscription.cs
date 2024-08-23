using Apollo.Abstractions;
using Apollo.Configuration;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.Logging;

namespace Apollo.Providers.ASB;


public class AsbTopicSubscription: ISubscription
{
    private readonly ApolloConfig apolloConfig;
    private readonly SubscriptionConfig subscriptionConfig;
    private readonly ServiceBusClient client;
    private readonly ILogger<AsbTopicSubscription> logger;
    private readonly BusResourceManager resourceManager;
    private readonly Func<ApolloContext, CancellationToken, Task> handler;
    private readonly bool isDurableConsumer;
    private readonly string topicName;
    private readonly Dictionary<string, Type> topicNameMapping;
    
    public AsbTopicSubscription(
        ApolloConfig apolloConfig,
        SubscriptionConfig subscriptionConfig,
        ServiceBusClient client,
        ILogger<AsbTopicSubscription> logger,
        BusResourceManager resourceManager,
        Func<ApolloContext, CancellationToken, Task> handler,
        bool isDurableConsumer = false
        )
    {
        this.apolloConfig = apolloConfig;
        this.subscriptionConfig = subscriptionConfig;
        this.client = client;
        this.logger = logger;
        this.resourceManager = resourceManager;
        this.handler = handler;
        this.isDurableConsumer = isDurableConsumer;
        
        topicName = Utils.GetTopic(subscriptionConfig);

        // trim .> and .* from the end of the subject
        var trimmedSubject = topicName.TrimWildEnds();
        topicNameMapping = subscriptionConfig.MessageTypes.ToDictionary(x => $"{trimmedSubject}.{x.Name.ToLower()}", x => x);

    }
    public async Task SubscribeAsync(CancellationToken cancellationToken)
    {
        try
        {
            // this was built for NATS so we need to strip the wildcard
            logger.LogWarning("Subscribing to {Topic}", topicName);
            logger.LogTrace("CreateMissingResources: {CreateMissingResources}", subscriptionConfig.CreateMissingResources);

            // ASB only lets the subscription name be 50 characters
            var safeConsumerLength = 50 - subscriptionConfig.ConsumerName.Length;
            var safeConsumerName = subscriptionConfig.ConsumerName.Length > safeConsumerLength
                ? subscriptionConfig.ConsumerName[..safeConsumerLength]
                : subscriptionConfig.ConsumerName;

            var subscriptionOptions = new CreateSubscriptionOptions(topicName, safeConsumerName)
            {
                // the original subscription name for full reference
                UserMetadata = subscriptionConfig.ConsumerName
            };

            if (!isDurableConsumer)
            {
                subscriptionOptions.DefaultMessageTimeToLive = TimeSpan.FromMinutes(1);
                subscriptionOptions.AutoDeleteOnIdle = TimeSpan.FromMinutes(5);
            }

            var topicExists = await resourceManager.TopicExistsAsync(subscriptionOptions.TopicName, cancellationToken);
            if (!topicExists)
            {
                if (!subscriptionConfig.CreateMissingResources) throw new InvalidOperationException($"Missing topic: {subscriptionOptions.TopicName}");
                logger.LogTrace("Creating topic {TopicName}", subscriptionOptions.TopicName);
                await resourceManager.CreateTopicAsync(subscriptionOptions.TopicName, cancellationToken);
            }

            var subscriptionExists = await resourceManager.SubscriptionExistsAsync(subscriptionOptions.TopicName,
                subscriptionOptions.SubscriptionName, cancellationToken);
            if (!subscriptionExists)
            {
                if (!subscriptionConfig.CreateMissingResources) 
                    throw new InvalidOperationException($"Missing subscription ({subscriptionOptions.SubscriptionName}) on topic ({subscriptionOptions.SubscriptionName})");
                
                logger.LogTrace("Creating subscription {SubscriptionName} on topic {TopicName}",
                    subscriptionOptions.SubscriptionName, subscriptionOptions.TopicName);
                await resourceManager.CreateSubscriptionAsync(subscriptionOptions, cancellationToken);
            }

            var identifier = apolloConfig.InstanceId;
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
            logger.LogError(ex, "Error subscribing to {EndpointSubject}", subscriptionConfig.EndpointSubject);
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
            var messageTypeMetaData = msg.ApplicationProperties["message-type"].ToString();
            var message = new ApolloMessage
            {
                Subject = $"{msg.Subject}.{messageTypeMetaData}",
                //Headers = msg.ApplicationProperties,
                Data = msg.Body.ToArray()
            };

            if (msg.Body != null)
            {
                var json = msg.Body.ToString();
                logger.LogTrace("JSON:\n{Json}", json);

                topicNameMapping.TryGetValue(message.Subject, out var messageType);
                message.MessageType = messageType;

                var replyFunc = msg.ReplyTo != null
                    ? new Func<byte[], CancellationToken, Task>(
                        (response, innerCancel) =>
                        {
                            var sender = client.CreateSender($"{msg.ReplyTo}.{msg.ReplyToSessionId}");
                            var responseMessage = new ServiceBusMessage(response)
                            {
                                SessionId = msg.ReplyToSessionId // Set the SessionId to the ReplyToSessionId from the request
                            };
                            return sender.SendMessageAsync(responseMessage, innerCancel);
                        }
                    )
                    : null;

                return handler(new ApolloContext(message, replyFunc), cancellationToken);
            }
            
            return Task.CompletedTask;
        }
    }
}