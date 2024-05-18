using System.Collections.Concurrent;
using Apollo.Messaging.Abstractions;

namespace Apollo.Messaging.Endpoints;

public interface IEndpointRegistry
{
    void RegisterEndpoint(EndpointRegistration registration);

    IEnumerable<EndpointRegistration> GetEndpointsForCommand<TCommand>(
        Func<EndpointRegistration, bool>? predicate = default) where TCommand : ICommand;

    IEnumerable<EndpointRegistration> GetEndpointsForEvent<TEvent>(
        Func<EndpointRegistration, bool>? predicate = default) where TEvent : IEvent;

    IEnumerable<EndpointRegistration> GetEndpointsForRequest<TRequest>(
        Func<EndpointRegistration, bool>? predicate = default) where TRequest : IRequest;

    IEnumerable<EndpointRegistration> GetEndpointRegistrations(Func<EndpointRegistration, bool>? predicate = default);
    
    public IReadOnlyList<Type> SubscriberTypes { get; }
}

internal class EndpointRegistry : IEndpointRegistry
{
    public IReadOnlyList<Type> SubscriberTypes => subscriberTypes;
    private readonly ConcurrentDictionary<Type, List<EndpointRegistration>> messageEndpoints = new();
    private readonly List<Type> subscriberTypes = new();

    public void RegisterEndpoint(EndpointRegistration registration)
    {
        var handlerTypes = registration.EndpointType.MessageHandlerTypes();
        foreach (var handlerType in handlerTypes)
        {
            if (registration.Config.DurableConfig.IsDurableConsumer
                && handlerType.IsRequestHandler())
            {
                // Until we can figure out how to handle durable request handlers
                // or split request handlers from durable endpoints we'll throw an exception
                throw new Exception(
                    $"Request handlers cannot be durable consumers. Endpoint: {registration.EndpointType.Name}");
            }

            messageEndpoints.AddOrUpdate(handlerType,
                _ => [registration],
                (_, list) =>
                {
                    // multiple subscribers could register and subscribe to the same message type
                    // subscribers will be derived based on the types they handle
                    var skip = list.Any(x => x.EndpointType == registration.EndpointType);
                    if (skip)
                        throw new Exception("Did we hit this? Hope not. ;)"); //return list;

                    list.Add(registration);
                    return list;
                });
        }
    }
    
    public void AddSubscriberType<T>() where T : ISubscriber
    {
        var subscriberType = typeof(T);
        if (!subscriberTypes.Contains(subscriberType))
        {
            subscriberTypes.Add(subscriberType);
        }
    }

    public IEnumerable<EndpointRegistration> GetEndpointsForCommand<TCommand>(
        Func<EndpointRegistration, bool>? predicate = default) where TCommand : ICommand
        => GetEndpointRegistrations(typeof(IHandle<TCommand>), predicate);

    public IEnumerable<EndpointRegistration> GetEndpointsForEvent<TEvent>(
        Func<EndpointRegistration, bool>? predicate = default) where TEvent : IEvent
        => GetEndpointRegistrations(typeof(IListenFor<TEvent>), predicate);

    public IEnumerable<EndpointRegistration> GetEndpointsForRequest<TRequest>(
        Func<EndpointRegistration, bool>? predicate = default) where TRequest : IRequest
    {
        var requestType = typeof(TRequest);
        var responseType = requestType.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>))
            ?.GetGenericArguments()[0];

        if (responseType == null)
        {
            throw new InvalidOperationException($"The request type {requestType.Name} does not have a response type.");
        }

        var replyToInterface = typeof(IReplyTo<,>).MakeGenericType(requestType, responseType);
        return GetEndpointRegistrations(replyToInterface, predicate);
    }

    public IEnumerable<EndpointRegistration> GetEndpointRegistrations(Type handlerType,
        Func<EndpointRegistration, bool>? predicate = default)
    {
        var registrations = messageEndpoints.TryGetValue(handlerType, out var list)
            ? list
            : Enumerable.Empty<EndpointRegistration>();
        predicate ??= _ => true;
        return registrations.Where(predicate);
    }

    public IEnumerable<EndpointRegistration> GetEndpointRegistrations(
        Func<EndpointRegistration, bool>? predicate = default)
    {
        predicate ??= _ => true;
        return messageEndpoints.Values.SelectMany(regs => regs).Where(predicate).Distinct();
    }

    public bool SupportsSubscriberType<T>() where T : ISubscriber
    {
        var subscriberType = typeof(T);
        return subscriberTypes.Contains(subscriberType);
    }
}