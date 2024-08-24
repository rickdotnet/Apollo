using Apollo.Abstractions;

namespace Apollo.Configuration;

public record PublishConfig
{
    public string? Namespace { get; set; } // namespace will prefix endpoint name
    public string? EndpointName { get; init; } // endpoint name or subject must be provided
    public string? EndpointSubject { get; set; } // endpoint name or subject must be provided
    public IProviderPublisher? ProviderPublisher { get; set; } // optionally override the DI provider publisher
}