using Apollo.Abstractions.Messaging.Events;
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

    public Task HandleEventAsync(TestEvent message, CancellationToken cancellationToken = default)
    {
        logger1.LogInformation("MyOtherEndpoint: {Message}", message.Message);
        return Task.CompletedTask;
    }
}