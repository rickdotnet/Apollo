using Apollo.Messaging.Abstractions;

namespace Apollo.Messaging;

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
    public static bool IsRequest(this Type type) => type.ImplementsGenericInterface(typeof(IRequest<>));

    public static IEnumerable<Type> MessageHandlerTypes(this Type endpointType) =>
        endpointType.EventHandlerTypes()
            .Concat(endpointType.CommandHandlerTypes())
            .Concat(endpointType.RequestHandlerTypes());

    private static bool ImplementsInterface(this Type type, Type interfaceType) 
        => interfaceType.IsAssignableFrom(type);

    private static bool ImplementsGenericInterface(this Type type, Type genericInterfaceType) 
        => type.GetInterfaces().Any(it => it.IsGenericType && it.GetGenericTypeDefinition() == genericInterfaceType);

    private static IEnumerable<Type> EventHandlerTypes(this Type endpointType) 
        => endpointType.GetInterfaces()
            .Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IListenFor<>));

    private static IEnumerable<Type> CommandHandlerTypes(this Type endpointType) =>
        endpointType.GetInterfaces()
            .Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IHandle<>));

    private static IEnumerable<Type> RequestHandlerTypes(this Type endpointType) =>
        endpointType.GetInterfaces()
            .Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IReplyTo<,>));

    public static bool IsRequestHandler(this Type handlerType) 
        => handlerType.IsGenericType && handlerType.GetGenericTypeDefinition() == typeof(IReplyTo<,>);
}