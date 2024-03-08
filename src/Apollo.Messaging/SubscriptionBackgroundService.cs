using Apollo.Endpoints;
using Apollo.Messaging;
using Apollo.Nats;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;

namespace Apollo.Hosting;

public class SubscriptionBackgroundService : BackgroundService
{
    private readonly ILogger<SubscriptionBackgroundService> logger;
    private readonly IServiceScopeFactory scopeFactory;

    public SubscriptionBackgroundService(IServiceProvider serviceProvider)
    {
        logger = serviceProvider.GetRequiredService<ILogger<SubscriptionBackgroundService>>();
        scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Starting NATS subscription background service");
        var scope = scopeFactory.CreateScope();
        var endpointRegistry = scope.ServiceProvider.GetRequiredService<IEndpointRegistry>();
        var localPublisher = scope.ServiceProvider.GetRequiredService<ILocalPublisher>();
        var connection = scope.ServiceProvider.GetRequiredService<INatsConnection>();

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
                    ? new NatsJetStreamSubscriber(connection, config, scope.GetLogger<NatsJetStreamSubscriber>(),
                        stoppingToken)
                    : new NatsCoreSubscriber(connection, config, scope.GetLogger<NatsCoreSubscriber>(), stoppingToken)
            );
        }

        // each subscriber will use the same handler that will dispatch the message to the correct endpoint
        var tasks = subscribers.Select(subscriber => subscriber.SubscribeAsync(Handler));

        // start all subscribers
        await Task.WhenAll(tasks);

        logger.LogInformation("NATS subscription background service task completed");
        return;

        async Task<bool> Handler(NatsMessageReceivedEvent message, CancellationToken cancellationToken)
        {
            // TODO: figure out if we need this extensibility point or if we can simply fire and forget
            await localPublisher.BroadcastAsync(message, cancellationToken);
            return true;
        }
    }
}