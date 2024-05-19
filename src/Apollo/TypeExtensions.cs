using Apollo.Configuration;

namespace Apollo;

public static class TypeExtensions
{
    public static Type GetMessageType(this Type handlerType)
    {
        // TODO: make this safer
        return handlerType.GetGenericArguments()[0];
    }
    public static Type? GetMessageType(this IDictionary<string,Type> subjectTypeMapping, string subject)
    {
        return subjectTypeMapping.TryGetValue(subject, out var type)
            ? type.GetMessageType() 
            : null;
    }
    public static Type? GetMessageType(this SubscriptionConfig config, string subject)
    {
        return config.MessageTypes.GetMessageType(subject);
    }
}