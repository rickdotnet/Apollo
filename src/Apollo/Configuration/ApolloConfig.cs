namespace Apollo.Configuration;

/// <summary>
/// Configure Apollo
/// </summary>
/// <param name="url">Defaults to "nats://localhost:4222"</param>
public class ApolloConfig
{
    public ApolloConfig() { }
    public ApolloConfig(string url) { Url = url; }
    public string Url { get; set; } = "nats://localhost:4222";
    public string ConsumerName { get; set; } = "DefaultConsumer";
    public string DefaultNamespace { get; set; } = "apollo.default";

    public string? Jwt { get; set; }

    public string? Seed { get; set; }

    public string? NKey { get; }

    public string? Token { get; }
    
    public static ApolloConfig Default => new();
}