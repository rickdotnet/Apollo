using Apollo.Messaging.Abstractions;
using Apollo.Messaging.Contracts;
using Microsoft.Extensions.Logging;

namespace TestHost;

public class MyOtherEndpoint : IListenFor<TestEvent>
{
    private readonly ILogger<MyOtherEndpoint> logger1;

    public MyOtherEndpoint(ILogger<MyOtherEndpoint> logger)
    {
        logger1 = logger;
    }

    public Task HandleAsync(TestEvent message, CancellationToken cancellationToken = default)
    {
        logger1.LogInformation("MyOtherEndpoint: {Message}", message.Message);
        return Task.CompletedTask;
    }
}