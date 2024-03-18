namespace Apollo;

public static class TypeExtensions
{
    public static Type GetMessageType(this Type handlerType)
    {
        // TODO: make this safer
        return handlerType.GetGenericArguments()[0];
    }
}