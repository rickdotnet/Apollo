using Apollo.Configuration;
using Apollo.Messaging.Endpoints;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Apollo.Messaging;

public class SubscriptionBackgroundService : BackgroundService
{
    private readonly ILogger<SubscriptionBackgroundService> logger;
    private readonly IServiceProvider serviceProvider;
    private readonly MessageProcessor requestProcessor;
    private Task processorTask = Task.CompletedTask; // hang out

    public SubscriptionBackgroundService(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
        logger = serviceProvider.GetRequiredService<ILogger<SubscriptionBackgroundService>>();
        requestProcessor = serviceProvider.GetRequiredService<MessageProcessor>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Starting NATS subscription background service");

        var endpointRegistries = serviceProvider.GetRequiredService<IEnumerable<IEndpointRegistry>>();

        processorTask = CreateRequestProcessor(stoppingToken);

        var subscribers = new List<ApolloSubscriber>();

        foreach (var endpointRegistry in endpointRegistries)
        {
            // remote endpoints only
            var endpoints = endpointRegistry.GetEndpointRegistrations(x => x.Config.IsRemoteEndpoint);

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

                var config = new SubscriptionConfig
                {
                    Namespace = endpoint.Config.Namespace,
                    EndpointType = endpoint.EndpointType,
                    EndpointName = endpointName,
                    MessageTypes = subjectTypeMapping,
                    EndpointSubject = filterSubject,
                    ConsumerName = endpoint.Config.ConsumerName,
                    IsDurableConsumer = endpoint.Config.DurableConfig?.IsDurableConsumer ?? false,
                    Serializer = null // get from service provider
                };

                var newSubscribers = endpointRegistry.SubscriberTypes.Select(
                    subscriberType => new ApolloSubscriber
                    {
                        Identifier = endpoint.EndpointRoute,
                        Config = config,
                        Subscriber = (ISubscriber)serviceProvider.GetRequiredService(subscriberType),
                        
                    }).ToArray();

                if (newSubscribers.Any())
                    subscribers.AddRange(newSubscribers);
                else
                {
                    // protect against no explicit subscriber registration
                    // and get the one registered in the service provider
                    var subs = serviceProvider.GetService<IEnumerable<ISubscriber>>()?.ToArray() ?? [];
                    if (!subs.Any())
                        throw new Exception(
                            "No SubscriberTypes registered and no subscriber found in service provider");

                    subscribers.AddRange(subs.Select(x => new ApolloSubscriber
                        {
                            Identifier = endpoint.EndpointRoute,
                            Config = config,
                            Subscriber = x
                        })
                    );
                }
            }
        }


        // each subscriber will use the same handler that will dispatch the message to the correct endpoint
        var tasks = subscribers.Select(
            apolloSub => apolloSub.Subscriber.SubscribeAsync(apolloSub.Config, Handler, stoppingToken));

        // start all subscribers
        await Task.WhenAll(tasks);

        logger.LogInformation("NATS subscription background service task completed");
        return;

        async Task Handler(ApolloMessage message, CancellationToken cancellationToken)
        {
            logger.LogInformation("EnqueueMessageAsync message of type {MessageType}", message.Message?.GetType().Name);

            var headers =
                message.Headers?
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value.Aggregate((x, y) => $"{x},{y}")
                    );

            headers ??= new Dictionary<string, string?>();

            // messages that come from outside of Apollo get an Id
            // if (!headers.ContainsKey("Message-Id"))
            //     headers.Add("Message-Id", idGenerator.CreateId().ToString());

            await requestProcessor.EnqueueMessageAsync(
                new MessageContext
                {
                    Headers = headers!,
                    ReplyTo = message.ReplyTo,
                    Replier = message.Replier,
                    Subject = message.Subject,
                    Message = message.Message,
                    Source = "NATS",
                }, cancellationToken);
        }
    }

    private Task CreateRequestProcessor(CancellationToken stoppingToken)
    {
        return requestProcessor.StartProcessingAsync(2,
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

internal class ApolloSubscriber
{
    public required string Identifier { get; init; }
    public required SubscriptionConfig Config { get; init; }
    public required ISubscriber Subscriber { get; init; }
}