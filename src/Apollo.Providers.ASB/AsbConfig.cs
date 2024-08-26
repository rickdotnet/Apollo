namespace Apollo.Providers.ASB;

public record AsbConfig
{
    public required string ConnectionString { get; init; }
    //public required string TenantId { get; set; }
    //public required string ClientId { get; set; }
    //public string? ClientSecret { get; set; }
    public string AuthorityHost { get; set; } = "https://login.windows.net"; // AzureAuthorityHosts.AzurePublicCloud
    
    // public required string TopicName { get; init; }
    // public required string SubscriptionName { get; init; }
    // public bool IsDurable => true;
    // public bool CreateMissingResources { get; set; }
}