using Apollo.Core.Messaging.Commands;
using Apollo.Core.Messaging.Events;
using Apollo.Core.Messaging.Requests;

namespace Apollo.Core.Endpoints;

internal static class TypeExtensions
{
    public static bool ImplementsInterface(this Type type, Type interfaceType)
    {
        return interfaceType.IsAssignableFrom(type);
    }

    public static bool ImplementsGenericInterface(this Type type, Type genericInterfaceType)
    {
        return type.GetInterfaces().Any(it => it.IsGenericType && it.GetGenericTypeDefinition() == genericInterfaceType);
    }

    public static Type? GetGenericInterface(this Type type, Type genericInterfaceType)
    {
        return type.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == genericInterfaceType);
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
    public static Type GetResponseType(this Type handlerType)
    {
        // TODO: make this safer
        return handlerType.GetGenericArguments()[1];
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
    public static bool IsListener(this Type handlerType)
    {
        return handlerType.IsGenericType && handlerType.GetGenericTypeDefinition() == typeof(IListenFor<>);
    }
    public static bool IsCommandHandler(this Type handlerType)
    {
        return handlerType.IsGenericType && handlerType.GetGenericTypeDefinition() == typeof(IHandle<>);
    }
    
    public static bool IsRequestHandler(this Type handlerType)
    {
        return handlerType.IsGenericType && handlerType.GetGenericTypeDefinition() == typeof(IReplyTo<,>);
    }
}