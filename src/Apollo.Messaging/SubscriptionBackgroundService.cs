using Apollo.Messaging.Endpoints;
using Apollo.Nats;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;

namespace Apollo.Messaging;

public class SubscriptionBackgroundService : BackgroundService
{
    private readonly ILogger<SubscriptionBackgroundService> logger;
    private readonly IServiceProvider serviceProvider;
    private readonly MessageProcessor requestProcessor;

    public SubscriptionBackgroundService(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
        logger = serviceProvider.GetRequiredService<ILogger<SubscriptionBackgroundService>>();
        requestProcessor = serviceProvider.GetRequiredService<MessageProcessor>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Starting NATS subscription background service");

        // both fine to grab from singleton scope
        var endpointRegistry = serviceProvider.GetRequiredService<IEndpointRegistry>();
        var connection = serviceProvider.GetRequiredService<INatsConnection>();

        StartRequestProcessor(stoppingToken);

        // remote endpoints only
        var endpoints = endpointRegistry.GetEndpointRegistrations(x => x.Config.IsRemoteEndpoint);
        var subscribers = new List<INatsSubscriber>();

        foreach (var endpoint in endpoints)
        {
            var endpointName = endpoint.Config.UseEndpointNameInRoute ? endpoint.EndpointType.Name : string.Empty;
            var filterSubject = $"{endpoint.Config.Namespace}.{endpointName}.>".ToLower();
            var subjectTypeMapping = new Dictionary<string, Type>();
            foreach (var handlerType in endpoint.HandlerTypes)
            {
                var subject =
                    $"{endpoint.Config.Namespace}.{endpointName}.{handlerType.GetMessageType().Name}".ToLower();

                subjectTypeMapping.Add(subject, handlerType);
            }

            var config = new NatsSubscriptionConfig
            {
                Namespace = endpoint.Config.Namespace,
                EndpointType = endpoint.EndpointType,
                EndpointName = endpointName,
                MessageTypes = subjectTypeMapping,
                EndpointSubject = filterSubject,
                ConsumerName = endpoint.Config.ConsumerName,
                Serializer = null, // get from service provider
                NatsSubOpts = null
            };

            subscribers.Add(
                endpoint.Config.DurableConfig.IsDurableConsumer
                    ? new NatsJetStreamSubscriber(
                        connection,
                        config,
                        serviceProvider.GetLogger<NatsJetStreamSubscriber>(),
                        stoppingToken)
                    : new NatsCoreSubscriber(
                        connection,
                        config,
                        serviceProvider.GetLogger<NatsCoreSubscriber>(),
                        stoppingToken)
            );
        }


        // each subscriber will use the same handler that will dispatch the message to the correct endpoint
        var tasks = subscribers.Select(subscriber => subscriber.SubscribeAsync(Handler));

        // start all subscribers
        await Task.WhenAll(tasks);

        logger.LogInformation("NATS subscription background service task completed");
        return;

        async Task<bool> Handler(NatsMessage message, CancellationToken cancellationToken)
        {
            logger.LogInformation("EnqueueMessageAsync message of type {MessageType}", message.Message?.GetType().Name);

            var headers = message.Headers?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Aggregate((x, y) => $"{x},{y}"));
            headers ??= new Dictionary<string, string?>();
            await requestProcessor.EnqueueMessageAsync(
                new MessageContext
                {
                    Headers = headers!,
                    ReplyTo = message.ReplyTo,
                    Subject = message.Subject,
                    Message = message.Message,
                    Source = "NATS"
                }, cancellationToken);

            return true;
        }
    }

    private void StartRequestProcessor(CancellationToken stoppingToken)
    {
        _ = requestProcessor.StartProcessingAsync(2,
                (_, message, cancellationToken) =>
                {
                    logger.LogWarning("Processing is complete for message of type {MessageType}",
                        message.Message?.GetType().Name);
                    return Task.CompletedTask;
                }, stoppingToken)
            .ContinueWith(task =>
            {
                if (task.Exception != null)
                {
                    logger.LogError(task.Exception, "An error occurred while processing messages");
                }
            }, TaskScheduler.Default);
    }
}