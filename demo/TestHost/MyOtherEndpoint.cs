using Apollo.Messaging.Abstractions;
using Apollo.Messaging.Contracts;
using Microsoft.Extensions.Logging;

namespace TestHost;

public class MyOtherEndpoint : IListenFor<DemoMessages>
{
    private readonly ILogger<MyOtherEndpoint> logger;

    public MyOtherEndpoint(ILogger<MyOtherEndpoint> logger)
    {
        this.logger = logger;
    }

    public Task HandleAsync(DemoMessages message, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("MyOtherEndpoint: {Message}", message.Message);
        return Task.CompletedTask;
    }
}