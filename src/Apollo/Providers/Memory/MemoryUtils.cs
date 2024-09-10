using Apollo.Configuration;

namespace Apollo.Providers.Memory;

// this is copied and modified from the NATS provider, because, lazy. 
// Will come up with a subject naming strategy later
internal static class MemoryUtils
{
    public static string GetSubject(PublishConfig config, Type? messageType = null)
    {
        return GetSubject((config.Namespace, config.EndpointName, EndpointType: null, config.EndpointSubject))
            .TrimWildEnds();// + $".{messageType?.Name.ToLower()}";
    }

    private static string TrimWildEnds(this string subject)
        => subject.TrimEnd('>').TrimEnd('*').TrimEnd('.');

    public static string GetSubject(SubscriptionConfig config)
        => GetSubject((config.Namespace, config.EndpointName, config.EndpointType, config.EndpointSubject));

    private static string GetSubject(
        (string? Namespace, string? EndpointName, Type? EndpointType, string? EndpointSubject) config)
    {
        if (string.IsNullOrEmpty(config.Namespace)
            && string.IsNullOrEmpty(config.EndpointName)
            && string.IsNullOrEmpty(config.EndpointSubject))
        {
            throw new ArgumentException("Namespace, EndpointName, or EndpointSubject must be set");
        }

        var endpoint = config.EndpointSubject;
        if (string.IsNullOrEmpty(endpoint))
            endpoint = Slugify(config.EndpointName);

        if (string.IsNullOrEmpty(endpoint))
            endpoint = Slugify(config.EndpointType?.Name);

        // var result = $"{config.Namespace}.{endpoint?.TrimWildEnds()}".ToLower();
        // if (config.EndpointType != null)
        //     result += ".>";

        return $"{config.Namespace}.{endpoint?.TrimWildEnds()}".ToLower();
    }

    private static string? Slugify(string? input)
    {
        return input?.ToLower().Replace(" ", "-");
    }

    public static string CleanStreamName(string streamName)
    {
        return streamName.Replace(".", "_")
            .Replace("*", "")
            .Replace(">", "")
            .TrimEnd('_');
    }
}