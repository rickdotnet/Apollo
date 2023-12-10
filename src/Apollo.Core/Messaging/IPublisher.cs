using System.Collections.Concurrent;
using System.Reflection;
using Apollo.Core.Messaging.Commands;
using Apollo.Core.Messaging.Events;
using Apollo.Core.Messaging.Requests;

namespace Apollo.Core.Messaging;

public interface IPublisher
{
    ValueTask SendCommandAsync<TCommand>(TCommand commandMessage, CancellationToken cancellationToken)
        where TCommand : ICommand;

    ValueTask BroadcastAsync<TEvent>(TEvent eventMessage, CancellationToken cancellationToken)
        where TEvent : IEvent;

    ValueTask<TResponse> SendRequestAsync<TRequest, TResponse>(TRequest requestMessage,
        CancellationToken cancellationToken)
        where TRequest : IRequest<TResponse>;
}

public static class PublisherExtensions
{
    private static readonly ConcurrentDictionary<(Type requestType, Type responseType), MethodInfo> requestMethodCache = new();
    private static readonly ConcurrentDictionary<Type, MethodInfo> broadcastMethodCache = new();
    public static ValueTask<TResult> SendRequestAsync<TResult>(this IPublisher publisher, IRequest<TResult> request,
        CancellationToken cancellationToken = default)
    {
        var requestType = request.GetType();
        var responseType = typeof(TResult);
        var cacheKey = (requestType, responseType);

        var method =
            requestMethodCache.GetOrAdd(cacheKey, key =>
            {
                var genericMethod =
                    typeof(IPublisher).GetMethod(nameof(IPublisher.SendRequestAsync),
                        BindingFlags.Instance | BindingFlags.Public);

                if (genericMethod == null)
                {
                    throw new InvalidOperationException(
                        "The SendRequestAsync method is not found on the IPublisher interface.");
                }

                return genericMethod.MakeGenericMethod(key.requestType, key.responseType);
            });

        var task = method.Invoke(publisher, new object[] { request, cancellationToken });

        if (task is ValueTask<TResult> resultTask)
            return resultTask;

        throw new InvalidOperationException(
            $"The result is not of the expected type ValueTask<{typeof(TResult).FullName}>.");
    }
    // public static ValueTask BroadcastFromRemoteAsync(this ILocalPublisher publisher, Type eventType, object eventMessage, CancellationToken cancellationToken = default)
    // {
    //     if (!typeof(IEvent).IsAssignableFrom(eventType))
    //         throw new ArgumentException("The provided type does not implement IEvent.", nameof(eventType));
    //
    //     var method = broadcastMethodCache.GetOrAdd(eventType, (Type key) =>
    //     {
    //         var genericMethod = typeof(ILocalPublisher).GetMethod(nameof(ILocalPublisher.BroadcastFromRemoteAsync), BindingFlags.Instance | BindingFlags.Public);
    //         if (genericMethod == null)
    //         {
    //             throw new InvalidOperationException("The BroadcastAsync method is not found on the IPublisher interface.");
    //         }
    //         return genericMethod.MakeGenericMethod(key);
    //     });
    //
    //     var result = method.Invoke(publisher, new object[] { eventMessage, cancellationToken });
    //     
    //     // Check for null before unboxing
    //     if (result == null)
    //         throw new InvalidOperationException("The result of the BroadcastAsync method cannot be null.");
    //
    //     var task = (ValueTask)result;
    //     return task;
    // }
}