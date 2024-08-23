using Apollo.Abstractions;

namespace Apollo.Internal;

internal static class TypeExtensions
{
    private static readonly Type[] SupportedInterfaces = {
        typeof(IListenFor<>),
        typeof(IHandle<>),
        typeof(IReplyTo<,>)
    };

    public static Type[] GetHandlerTypes(this Type endpointType)
    {
        var interfaces = endpointType.GetInterfaces();
        return interfaces.Where(i => i.IsGenericType && 
                                     SupportedInterfaces.Contains(i.GetGenericTypeDefinition()))
            .Select(i => i.GetGenericArguments()[0])
            .ToArray();
    }
    public static bool IsRequestHandler(this Type handlerType)
    {
        return handlerType.IsGenericType && handlerType.GetGenericTypeDefinition() == typeof(IReplyTo<,>);
    }
    
    public static bool IsRequest(this Type type)
    {
        return type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequest<>));
    }
}