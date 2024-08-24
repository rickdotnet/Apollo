namespace Apollo.Configuration;

/// <summary>
/// Configure Apollo
/// </summary>
/// <param name="url">Defaults to "nats://localhost:4222"</param>
public class ApolloConfig
{
    public string InstanceId { get; set; } = Guid.NewGuid().ToString();
    public string? ProviderUrl { get; set; } = "nats://localhost:4222";
    public string? DefaultConsumerName { get; set; }
    public string? DefaultNamespace { get; set; }
    public string? Username { get; set; } = "";
    public string? Password { get; set; } = "";
    
    /// <summary>
    /// Subscribers will create missing resources if set to true
    /// </summary>
    public bool CreateMissingResources { get; set; } = false;
    public static ApolloConfig Default => new(); }