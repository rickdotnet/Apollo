using System.Collections.Concurrent;
using System.Reflection;
using Apollo.Abstractions.Messaging.Commands;
using Apollo.Abstractions.Messaging.Events;
using Apollo.Abstractions.Messaging.Requests;
using Apollo.Core.Endpoints;

namespace Apollo.Core.Messaging;

public interface IApolloDispatcher
{
    ValueTask<TResponse> SendRequestToLocalEndpointsAsync<TRequest, TResponse>(TRequest requestMessage,
        CancellationToken cancellationToken)
        where TRequest : IRequest<TResponse>;

    ValueTask<TResponse> SendRequestToRemoteEndpointsAsync<TRequest, TResponse>(TRequest requestMessage,
        CancellationToken cancellationToken)
        where TRequest : IRequest<TResponse>;

    ValueTask SendCommandToLocalEndpointsAsync<TCommand>(TCommand commandMessage, CancellationToken cancellationToken)
        where TCommand : ICommand;

    ValueTask SendCommandToRemoteEndpointsAsync<TCommand>(TCommand commandMessage, CancellationToken cancellationToken)
        where TCommand : ICommand;
    
    ValueTask SendCommandToSingleRemoteEndpointsAsync<TCommand>(EndpointRegistration registration, TCommand commandMessage, CancellationToken cancellationToken)
        where TCommand : ICommand;

    ValueTask BroadcastToLocalEndpointsAsync<TEvent>(TEvent eventMessage, CancellationToken cancellationToken)
        where TEvent : IEvent;

    ValueTask BroadcastToRemoteEndpointsAsync<TEvent>(TEvent eventMessage, CancellationToken cancellationToken)
        where TEvent : IEvent;
    
    ValueTask BroadcastToSingleRemoteEndpointAsync<TEvent>(EndpointRegistration registration, TEvent eventMessage, CancellationToken cancellationToken)
        where TEvent : IEvent;
}

public static class ApolloDispatcherExtensions
{
    private static readonly ConcurrentDictionary<Type, MethodInfo> methodCache = new();

    public static ValueTask SendCommandToRemoteEndpointsAsync(this IApolloDispatcher dispatcher, Type commandType,
        object commandMessage, CancellationToken cancellationToken = default)
    {
        if (!commandType.ImplementsInterface(typeof(ICommand)))
            throw new ArgumentException("The provided type does not implement ICommand.", nameof(commandType));

        var method = GetOrAddDispatcherMethod(commandType, nameof(IApolloDispatcher.SendCommandToRemoteEndpointsAsync));
        return InvokeDispatcherMethod(dispatcher, method, commandMessage, cancellationToken);
    }
    public static ValueTask SendCommandToSingleRemoteEndpointsAsync(
        this IApolloDispatcher dispatcher,
        EndpointRegistration registration,
        Type commandType,
        object commandMessage, CancellationToken cancellationToken = default)
    {
        if (!commandType.ImplementsInterface(typeof(ICommand)))
            throw new ArgumentException("The provided type does not implement ICommand.", nameof(commandType));

        var method = GetOrAddDispatcherMethod(commandType, nameof(IApolloDispatcher.SendCommandToSingleRemoteEndpointsAsync));
        return InvokeDispatcherMethodForSingleRegistration(dispatcher, method, registration, commandMessage, cancellationToken);
    }

    public static ValueTask BroadcastToRemoteEndpointsAsync(this IApolloDispatcher dispatcher, Type eventType,
        object eventMessage, CancellationToken cancellationToken = default)
    {
        if (!eventType.ImplementsInterface(typeof(IEvent)))
            throw new ArgumentException("The provided type does not implement IEvent.", nameof(eventType));

        var method = GetOrAddDispatcherMethod(eventType, nameof(IApolloDispatcher.BroadcastToRemoteEndpointsAsync));
        return InvokeDispatcherMethod(dispatcher, method, eventMessage, cancellationToken);
    }
    
    public static ValueTask BroadcastToSingleRemoteEndpointAsync(
        this IApolloDispatcher dispatcher,
        EndpointRegistration registration,
        Type eventType,
        object eventMessage, CancellationToken cancellationToken = default)
    {
        if (!eventType.ImplementsInterface(typeof(IEvent)))
            throw new ArgumentException("The provided type does not implement IEvent.", nameof(eventType));

        var method = GetOrAddDispatcherMethod(eventType, nameof(IApolloDispatcher.BroadcastToSingleRemoteEndpointAsync));
        return InvokeDispatcherMethodForSingleRegistration(dispatcher, method, registration, eventMessage, cancellationToken);
    }

    public static ValueTask<object> SendRequestToRemoteEndpointsAsync(this IApolloDispatcher dispatcher, Type requestType,
        object requestMessage, CancellationToken cancellationToken = default)
    {
        if (!requestType.ImplementsGenericInterface(typeof(IRequest<>)))
            throw new ArgumentException("The provided type does not implement IRequest<> with a response.",
                nameof(requestType));

        var responseType = requestType.GetGenericInterface(typeof(IRequest<>))
                               ?.GetGenericArguments()[0]
                           ?? throw new InvalidOperationException("No IRequest<> interface found.");

        var method = GetOrAddDispatcherMethod(requestType, nameof(IApolloDispatcher.SendRequestToRemoteEndpointsAsync),
            responseType);
        
        return InvokeRequestDispatcherMethod(dispatcher, method, requestMessage, cancellationToken);
    }

    private static MethodInfo GetOrAddDispatcherMethod(Type messageType, string methodName, Type responseType = null)
    {
        return methodCache.GetOrAdd(messageType, (Type key) =>
        {
            var method = typeof(IApolloDispatcher).GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public);
            if (method == null)
                throw new InvalidOperationException(
                    $"The {methodName} method is not found on the IApolloDispatcher interface.");

            return responseType == null ? method.MakeGenericMethod(key) : method.MakeGenericMethod(key, responseType);
        });
    }

    private static ValueTask InvokeDispatcherMethod(IApolloDispatcher dispatcher, MethodInfo method, object message,
        CancellationToken cancellationToken)
    {
        var result = method.Invoke(dispatcher, new[] { message, cancellationToken });
        if (result == null)
            throw new InvalidOperationException("The result of the dispatcher method cannot be null.");

        return (ValueTask)result;
    }
    private static ValueTask InvokeDispatcherMethodForSingleRegistration(IApolloDispatcher dispatcher, MethodInfo method, EndpointRegistration registration, object message,
        CancellationToken cancellationToken)
    {
        var result = method.Invoke(dispatcher, new[] {registration, message, cancellationToken });
        if (result == null)
            throw new InvalidOperationException("The result of the dispatcher method cannot be null.");

        return (ValueTask)result;
    }
    private static async ValueTask<object> InvokeRequestDispatcherMethod(IApolloDispatcher dispatcher, MethodInfo method, object requestMessage, CancellationToken cancellationToken)
    {
        var result = method.Invoke(dispatcher, new[] { requestMessage, cancellationToken });
        if (result == null)
            throw new InvalidOperationException("The result of the dispatcher method cannot be null.");

        // Since we're using reflection, we don't know the exact type of the ValueTask (i.e., ValueTask<TResponse>).
        // We return an object here and cast it to the correct type in the calling method.
        // we need to await this here so the dynamic cast works
        return await (dynamic)result;
    }
}