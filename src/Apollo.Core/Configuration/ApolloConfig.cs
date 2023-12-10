namespace Apollo.Core.Configuration;

public class ApolloConfig(string url)
{
    public string Url { get; set; } = url;
    public string ConsumerName { get; set; } = "DefaultConsumer";
    public string DefaultNamespace { get; set; } = "apollo.default";
    public static ApolloConfig Default 
        => new("http://localhost:4222");
}