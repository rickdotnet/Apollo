using System.Threading.Channels;
using Apollo.Abstractions;
using Apollo.Configuration;
using Microsoft.Extensions.Logging;

namespace Apollo.Providers.Memory;

internal class InMemoryProvider : ISubscriptionProvider, IProviderPublisher
{
    public static readonly InMemoryProvider Instance = new();
    private readonly Dictionary<string, List<ChannelWriter<ApolloContext>>> subscriptions = new();
    private readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(30);

    public ISubscription AddSubscription(SubscriptionConfig config,
        Func<ApolloContext, CancellationToken, Task> handler)
    {
        var sub = new InMemorySubscription(handler);
        var subjectKey = MemoryUtils.GetSubject(config);

        var isHandlerOnly = config.EndpointType is null;
        if (isHandlerOnly)
        {
            if (!subscriptions.ContainsKey(subjectKey))
                subscriptions[subjectKey] = [];

            subscriptions[subjectKey].Add(sub.Writer);
        }
        else
        {
            foreach (var messageType in config.MessageTypes)
            {
                var compositeKey = $"{subjectKey}.{messageType?.Name}".ToLower();
                if (!subscriptions.ContainsKey(compositeKey))
                    subscriptions[compositeKey] = [];

                subscriptions[compositeKey].Add(sub.Writer);
            }
        }

        return sub;
    }

    public async Task PublishAsync(PublishConfig publishConfig, ApolloMessage message,
        CancellationToken cancellationToken)
    {
        var typeKey = MemoryUtils.GetSubject(publishConfig, message.MessageType);
        if (!subscriptions.TryGetValue(typeKey, out var subscription))
        {
            // no handlers for this message type?
            return;
        }

        var tasks = subscription.Select(
            async writer => { await writer.WriteAsync(new ApolloContext(message), cancellationToken); });

        await Task.WhenAll(tasks);
    }

    public async Task<byte[]> RequestAsync(PublishConfig publishConfig, ApolloMessage message, CancellationToken cancellationToken)
    {
        var typeKey = MemoryUtils.GetSubject(publishConfig, message.MessageType);
        if (!subscriptions.TryGetValue(typeKey, out var subscription))
            throw new InvalidOperationException("No handlers for this message type");

        var writer = subscription.First();

        var tcs = new TaskCompletionSource<byte[]>();
        var replyFunc = IsRequest(message.MessageType)
            ? new Func<byte[], CancellationToken, Task>(
                (response, _) =>
                {
                    tcs.TrySetResult(response);
                    return Task.CompletedTask;
                }
            )
            : null;

        await using (cancellationToken.Register(() => tcs.TrySetCanceled()))
        {
            await writer.WriteAsync(new ApolloContext(message, replyFunc), cancellationToken);
        
            var timeoutTask = Task.Delay(RequestTimeout, cancellationToken);
            var completedTask = await Task.WhenAny(tcs.Task, timeoutTask).ConfigureAwait(false);

            if (completedTask == timeoutTask)
            {
                tcs.TrySetCanceled(cancellationToken);
                throw new TimeoutException("The request timed out.");
            }

            // tcs.Task is completed above
            return tcs.Task.Result; 
        }
    }

    private static bool IsRequest(Type? type)
    {
        return type?.GetInterfaces()
            .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>)) is true;
    }
}