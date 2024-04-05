using Apollo.Messaging.Endpoints;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Apollo.Messaging.Wiretap;

public class WiretapExecutor
{
    private readonly IEndpointRegistry endpointRegistry;
    private readonly IServiceProvider scopedProvider;
    private ILogger<WiretapExecutor> logger;

    public WiretapExecutor(IEndpointRegistry endpointRegistry, IServiceProvider scopedProvider, ILogger<WiretapExecutor> logger)
    {
        this.endpointRegistry = endpointRegistry;
        this.scopedProvider = scopedProvider ?? throw new ArgumentNullException(nameof(scopedProvider));
        this.logger = logger;
    }

    public async Task ExecuteAsync(MessageContext messageContext, CancellationToken cancellationToken = default)
    {
        var wiretapRegistrations = endpointRegistry.GetEndpointsWithWiretaps(messageContext.Subject);

        foreach (var registration in wiretapRegistrations)
        {
            var wiretaps = registration.WiretapTypes.Select(x => scopedProvider.GetRequiredService(x));
            foreach (var wiretapType in registration.WiretapTypes)
            {
                var handleMethod = wiretapType.GetMethod("HandleAsync");
                if (handleMethod == null)
                {
                    logger.LogError("No handle method found for wiretap type {WiretapType}", wiretapType);
                    continue;
                }

                var implementation = scopedProvider.GetRequiredService(wiretapType);
                await (Task)handleMethod.Invoke(implementation, [messageContext.Message, cancellationToken])!;
            }
        }
    }
}