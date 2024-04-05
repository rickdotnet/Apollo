using System.Collections.Concurrent;
using Apollo.Messaging.Abstractions;

namespace Apollo.Messaging.Endpoints;

public interface IEndpointRegistry
{
    void RegisterEndpoint(IEndpointRegistration registration);

    IEnumerable<IEndpointRegistration> GetEndpointsForCommand<TCommand>(
        Func<IEndpointRegistration, bool>? predicate = default) where TCommand : ICommand;

    IEnumerable<IEndpointRegistration> GetEndpointsForEvent<TEvent>(
        Func<IEndpointRegistration, bool>? predicate = default) where TEvent : IEvent;

    IEnumerable<IEndpointRegistration> GetEndpointsForRequest<TRequest>(
        Func<IEndpointRegistration, bool>? predicate = default) where TRequest : IRequest;
    
    IEnumerable<IEndpointRegistration> GetEndpointRegistrations(Func<IEndpointRegistration, bool>? predicate = default);
    IEnumerable<IEndpointRegistration> GetEndpointsWithWiretaps(string subject);
}

internal class EndpointRegistry : IEndpointRegistry
{
    private readonly ConcurrentDictionary<Type, List<IEndpointRegistration>> messageEndpoints = new();

    public void RegisterEndpoint(IEndpointRegistration registration)
    {
        var handlerTypes = registration.EndpointType.MessageHandlerTypes();
        foreach (var handlerType in handlerTypes)
        {
            if (registration.Config.DurableConfig.IsDurableConsumer
                && handlerType.IsRequestHandler())
            {
                // Until we can figure out how to handle durable request handlers
                // or split request handlers from durable endpoints we'll throw an exception
                throw new Exception($"Request handlers cannot be durable consumers. Endpoint: {registration.EndpointType.Name}");
            }
            
            messageEndpoints.AddOrUpdate(handlerType,
                _ => [registration],
                (_, list) =>
                {
                    list.Add(registration);
                    return list;
                });
        }
    }

    public IEnumerable<IEndpointRegistration> GetEndpointsWithWiretaps(string subject)
        => messageEndpoints.Values.SelectMany(regs => regs)
            .Where(reg => reg is EndpointRegistration regImpl 
                          && regImpl.WiretapTypes.Any()
                          && reg.SubjectMapping.ContainsKey(subject)).Distinct();
    
    public IEnumerable<IEndpointRegistration> GetEndpointsForCommand<TCommand>(
        Func<IEndpointRegistration, bool>? predicate = default) where TCommand : ICommand
        => GetIEndpointRegistrations(typeof(IHandle<TCommand>), predicate);

    public IEnumerable<IEndpointRegistration> GetEndpointsForEvent<TEvent>(
        Func<IEndpointRegistration, bool>? predicate = default) where TEvent : IEvent
        => GetIEndpointRegistrations(typeof(IListenFor<TEvent>), predicate);

    public IEnumerable<IEndpointRegistration> GetEndpointsForRequest<TRequest>(
        Func<IEndpointRegistration, bool>? predicate = default) where TRequest : IRequest
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
        return GetIEndpointRegistrations(replyToInterface, predicate);
    }

    public IEnumerable<IEndpointRegistration> GetIEndpointRegistrations(Type handlerType,
        Func<IEndpointRegistration, bool>? predicate = default)
    {
        var registrations = messageEndpoints.TryGetValue(handlerType, out var list) ? list : Enumerable.Empty<IEndpointRegistration>();
        predicate ??= _ => true;
        return registrations.Where(predicate);
    }

    public IEnumerable<IEndpointRegistration> GetEndpointRegistrations(Func<IEndpointRegistration, bool>? predicate = default)
    {
        predicate ??= _ => true;
        return messageEndpoints.Values.SelectMany(regs => regs).Where(predicate).Distinct();
    }
}