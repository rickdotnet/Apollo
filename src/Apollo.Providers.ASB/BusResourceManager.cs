using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;

namespace Apollo.Providers.ASB;

// might end up being a bit more than just a wrapper
// isolating the resource management logic for now
public class BusResourceManager
{
    private readonly ServiceBusClient client;
    private readonly ServiceBusAdministrationClient adminClient;

    public BusResourceManager(ServiceBusClient client, ServiceBusAdministrationClient adminClient)
    {
        this.client = client;
        this.adminClient = adminClient;
    }

    public async Task<bool> QueueExistsAsync(string name, CancellationToken cancellationToken = default)
    {
        // we'll eventually be caching these results
        // await is to avoid the goofiness until then
        return await adminClient.QueueExistsAsync(name, cancellationToken);
    }

    public async Task<bool> TopicExistsAsync(string topicName, CancellationToken cancellationToken)
    {
        return await adminClient.TopicExistsAsync(topicName, cancellationToken);
    }

    public Task CreateQueueAsync(string queueName, CancellationToken cancellationToken)
        => adminClient.CreateQueueAsync(queueName, cancellationToken);

    public Task CreateTopicAsync(string topicName, CancellationToken cancellationToken)
        => adminClient.CreateTopicAsync(topicName, cancellationToken);

    public Task CreateSubscriptionAsync(CreateSubscriptionOptions createSubscriptionOptions,
        CancellationToken cancellationToken)
    {
        return adminClient.CreateSubscriptionAsync(createSubscriptionOptions, cancellationToken);
    }

    public async Task<bool> SubscriptionExistsAsync(string topicName, string subscriptionName,
        CancellationToken cancellationToken)
    {
        return await adminClient.SubscriptionExistsAsync(topicName, subscriptionName, cancellationToken);
    }

    public async Task CreateReplyTopicAndSubscriptionAsync(string topicName, string subscriptionName,
        CancellationToken cancellationToken)
    {
        // pass in filters based on handled message types
        // then create a rule
        // var rule = new CreateRuleOptions()
        // {
        //     Name = "RequestTypeFilter",
        //     Filter = new SqlRuleFilter($"Subject IN '{requestTypes}'")
        // };

        var subscriptionOptions = new CreateSubscriptionOptions(topicName, subscriptionName) { RequiresSession = true };

        if (!await TopicExistsAsync(topicName, cancellationToken))
            await CreateTopicAsync(topicName, cancellationToken);

        if (!await SubscriptionExistsAsync(topicName, subscriptionName, cancellationToken))
            await CreateSubscriptionAsync(subscriptionOptions, cancellationToken);
    }
}