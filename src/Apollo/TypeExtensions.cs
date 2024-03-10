using Apollo.Abstractions.Messaging.Commands;
using Apollo.Abstractions.Messaging.Events;
using Apollo.Abstractions.Messaging.Requests;

namespace Apollo;

public static class TypeExtensions
{
    public static bool IsCommand(this Type type)
    {
        return type.ImplementsInterface(typeof(ICommand));
    }
    public static bool IsEvent(this Type type)
    {
        return type.ImplementsInterface(typeof(IEvent));
    }
    public static bool IsRequest(this Type type)
    {
        return type.ImplementsGenericInterface(typeof(IRequest<>));
    }
 
    
    public static IEnumerable<Type> MessageHandlerTypes(this Type endpointType)
    {
        return
            endpointType.EventHandlerTypes()
                .Concat(endpointType.CommandHandlerTypes())
                .Concat(endpointType.RequestHandlerTypes());
    }

    public static Type GetMessageType(this Type handlerType)
    {
        // TODO: make this safer
        return handlerType.GetGenericArguments()[0];
    }
    
    private static bool ImplementsInterface(this Type type, Type interfaceType)
    {
        return interfaceType.IsAssignableFrom(type);
    }

    private static bool ImplementsGenericInterface(this Type type, Type genericInterfaceType)
    {
        return type.GetInterfaces().Any(it => it.IsGenericType && it.GetGenericTypeDefinition() == genericInterfaceType);
    }
    
    private static IEnumerable<Type> EventHandlerTypes(this Type endpointType)
    {
        return endpointType.GetInterfaces()
            .Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IListenFor<>));
    }

    private static IEnumerable<Type> CommandHandlerTypes(this Type endpointType)
    {
        return endpointType.GetInterfaces()
            .Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IHandle<>));
    }
    
    private static IEnumerable<Type> RequestHandlerTypes(this Type endpointType)
    {
        return endpointType.GetInterfaces()
            .Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IReplyTo<,>));
    }
    
    public static bool IsRequestHandler(this Type handlerType)
    {
        return handlerType.IsGenericType && handlerType.GetGenericTypeDefinition() == typeof(IReplyTo<,>);
    }
}