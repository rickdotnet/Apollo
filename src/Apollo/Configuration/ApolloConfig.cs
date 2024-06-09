namespace Apollo.Configuration;

/// <summary>
/// Configure Apollo
/// </summary>
/// <param name="url">Defaults to "nats://localhost:4222"</param>
public class ApolloConfig
{
    public ApolloConfig() { }
    public ApolloConfig(string url) { Url = url; }
    public Guid InstanceId { get; set; } = Guid.Parse("3c083d61-5a40-4476-af60-c200bd67772d");
    public string Url { get; set; } = "nats://localhost:4222";
    public string ConsumerName { get; set; } = "default-consumer";
    public string DefaultNamespace { get; set; } = "dev";
    
    /// <summary>
    /// Subscribers will create missing resources if set to true
    /// </summary>
    public bool CreateMissingResources { get; set; } = false;
    public static ApolloConfig Default => new(); }