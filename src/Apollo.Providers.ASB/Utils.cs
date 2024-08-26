using Apollo.Configuration;

namespace Apollo.Providers.ASB;

internal static class Utils
{
    public static string GetTopic(PublishConfig config, bool toLower = true)
        => GetTopic((config.Namespace, config.EndpointName, EndpointType: null, config.EndpointSubject))
            .TrimWildEnds();

    public static string GetTopic(SubscriptionConfig config, bool toLower = true)
        => GetTopic((config.Namespace, config.EndpointName, config.EndpointType, config.EndpointSubject));

    private static string GetTopic(
        (string? Namespace, string? EndpointName, Type? EndpointType, string? EndpointSubject) config, bool toLower = true)
    {
        // this entire class is carry over from POC
        // need to take a proper look at Apollo subject mapping
        // and stop forcing square pegs into round holes
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


        var result = $"{endpoint?.TrimWildEnds()}";
        if (!string.IsNullOrEmpty(config.Namespace))
            result = $"{config.Namespace}.{result}";
        
        return toLower ? result.ToLower() : result;

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