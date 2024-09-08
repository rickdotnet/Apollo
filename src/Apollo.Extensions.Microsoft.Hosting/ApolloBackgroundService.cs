using Apollo.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Apollo.Extensions.Microsoft.Hosting;

internal sealed class ApolloBackgroundService : BackgroundService, IAsyncDisposable
{
    private readonly IEnumerable<IEndpointRegistration> registrations;
    private readonly ApolloClient apolloClient;
    private readonly ILogger<ApolloBackgroundService> logger;
    private List<IApolloEndpoint> endpoints = new();

    public ApolloBackgroundService(
        IEnumerable<IEndpointRegistration> registrations,
        ApolloClient apolloClient,
        ILogger<ApolloBackgroundService> logger)
    {
        this.registrations = registrations;
        this.apolloClient = apolloClient;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("ApolloBackgroundService is starting.");
        
        try
        {
            foreach (var registration in registrations)
            {
                if (registration.EndpointType is null && registration.Handler is null)
                {
                    logger.LogWarning("{EndpointName} - EndpointType and Handler are both null. Skipping registration.", registration.Config.EndpointName);
                    continue;
                }

                logger.LogInformation("Starting Endpoint - {EndpointName}.", registration.Config.EndpointName);

                var endpoint = registration.IsHandler
                    ? apolloClient.AddHandler(registration.Config, registration.Handler!)
                    : apolloClient.AddEndpoint(registration.EndpointType!, registration.Config);
                
                endpoints.Add(endpoint);

                await endpoint.StartEndpoint(stoppingToken);
                logger.LogDebug("Endpoint ({EndpointName}) started successfully.", registration.Config.EndpointName);
            }

            await Task.Delay(-1, stoppingToken);
        }
        catch (TaskCanceledException)
        {
            logger.LogWarning("ApolloBackgroundService was canceled.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred during ExecuteAsync.");
        }
        
        logger.LogInformation("ApolloBackgroundService has finished execution.");
    }

    public async ValueTask DisposeAsync()
    {
        logger.LogInformation("ApolloBackgroundService is disposing.");
        
        foreach (var endpoint in endpoints)
        {
            try
            {
                await endpoint.DisposeAsync();
                logger.LogDebug("Endpoint disposed successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while disposing an endpoint.");
            }
        }
        
        logger.LogInformation("ApolloBackgroundService has been disposed.");
    }
}