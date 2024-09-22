using Apollo.Configuration;

namespace Apollo.Providers.NATS;

internal static class Utils
{
    public static string GetSubject(PublishConfig config)
        => GetSubject((config.Namespace, config.EndpointName, EndpointType: null, config.Subject))
            .TrimWildEnds();

    public static string GetSubject(SubscriptionConfig config)
        => GetSubject((config.Namespace, config.EndpointName, config.EndpointType, config.Subject));

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
        var explicitSubject = !string.IsNullOrEmpty(endpoint);
        if (string.IsNullOrEmpty(endpoint))
            endpoint = Slugify(config.EndpointName);

        if (string.IsNullOrEmpty(endpoint))
            endpoint = Slugify(config.EndpointType?.Name);

        if (!string.IsNullOrWhiteSpace(config.Namespace))
        {
            endpoint = !string.IsNullOrWhiteSpace(endpoint) 
                ? $"{config.Namespace}.{endpoint}" 
                : config.Namespace;
        }

        if (string.IsNullOrWhiteSpace(endpoint))
            throw new ArgumentException("Endpoint could not be determined");

        if (!explicitSubject)
            endpoint = $"{endpoint.TrimWildEnds()}.>";

        // temp fix for NATS case sensitivity
        return endpoint.StartsWith('$')
            ? endpoint.ToUpper()
            : endpoint;
    }

    private static string? Slugify(string? input)
    {
        return input?.ToLower().Replace(" ", "-");
    }

    /// <summary>
    /// Trims '.>' and '.*' from the end of the string
    /// </summary>
    /// <param name="subject"></param>
    /// <returns></returns>
    public static string TrimWildEnds(this string subject)
        => subject.TrimEnd('>').TrimEnd('*').TrimEnd('.');

    public static string CleanStreamName(this string streamName)
    {
        return streamName.Replace(".", "_")
            .Replace("*", "")
            .Replace(">", "")
            .TrimEnd('_');
    }
}