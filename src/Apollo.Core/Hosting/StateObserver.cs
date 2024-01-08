using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Apollo.Core.Hosting;

public interface IStateObserver
{
    IDisposable Register<TStateChange>(Func<TStateChange, Task> callback);
    Task NotifyAsync<TStateChange>(TStateChange stateChange, CancellationToken cancellationToken = default);
}

public class StateObserver : IStateObserver
{
    private readonly ConcurrentDictionary<Type, ConcurrentDictionary<Guid, Delegate>> subscriptions =
        new();

    private readonly ILogger<StateObserver> logger;

    public StateObserver(ILogger<StateObserver> logger)
    {
        this.logger = logger;
    }

    public IDisposable Register<TStateChange>(Func<TStateChange, Task> callback)
    {
        var subscriptionType = typeof(TStateChange);
        var subscriptionDictionaries = subscriptions.GetOrAdd(subscriptionType,
            _ => new ConcurrentDictionary<Guid, Delegate>());

        var subscriptionId = Guid.NewGuid();
        subscriptionDictionaries.TryAdd(subscriptionId, callback);

        return new SubscriptionHandle(() => subscriptionDictionaries.TryRemove(subscriptionId, out _));
    }

    public async Task NotifyAsync<TStateChange>(TStateChange stateChange, CancellationToken cancellationToken = default)
    {
        var subscriptionType = typeof(TStateChange);
        if (subscriptions.TryGetValue(subscriptionType, out var subscriptionDictionary))
        {
            var subscribers = subscriptionDictionary.Values.Cast<Func<TStateChange, Task>>().ToList();
            var tasks = subscribers.Select(subscriber => SafeNotifySubscriber(subscriber, stateChange, cancellationToken));
            await Task.WhenAll(tasks);
        }
    }

    private async Task SafeNotifySubscriber<TStateChange>(
        Func<TStateChange, Task> subscriber,
        TStateChange stateChange,
        CancellationToken cancellationToken)
    {
        try
        {
            // await here instead of making every caller implement a try/catch
            await subscriber(stateChange);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Cancellation requested during notification for {StateChange}", typeof(TStateChange).Name);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error notifying subscriber for {StateChange}", typeof(TStateChange).Name);
        }
    }

    private class SubscriptionHandle : IDisposable
    {
        private readonly Action onDispose;
        private int isDisposed;

        public SubscriptionHandle(Action onDispose)
        {
            this.onDispose = onDispose;
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref isDisposed, 1) == 0)
            {
                onDispose?.Invoke();
            }
        }
    }
}