using Apollo.Configuration;

namespace Apollo.Providers.ASB;

internal static class Utils
{
    public static string GetTopic(PublishConfig config)
        => GetTopic((config.Namespace, config.EndpointName, EndpointType: null, config.EndpointSubject))
            .TrimWildEnds();

    public static string GetTopic(SubscriptionConfig config)
        => GetTopic((config.Namespace, config.EndpointName, config.EndpointType, config.EndpointSubject));

    private static string GetTopic(
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

        var result = $"{config.Namespace}.{endpoint?.TrimWildEnds()}".ToLower();

        return result;
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