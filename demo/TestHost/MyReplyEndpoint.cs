using Apollo.Messaging.Abstractions;
using Apollo.Messaging.Endpoints;
using Microsoft.Extensions.Logging;

namespace TestHost;

public record MyRequest(string Message) : IRequest<bool>;

public class MyReplyEndpoint : EndpointBase, IReplyTo<MyRequest, bool>
{
    private readonly ILogger<MyReplyEndpoint> logger;

    public MyReplyEndpoint(ILogger<MyReplyEndpoint> logger)
    {
        this.logger = logger;
    }

    public Task<bool> HandleAsync(MyRequest message, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("MyReplyEndpoint Received: {Message}", message.Message);
        logger.LogTrace("Returning true");
        return Task.FromResult(true);
    }
}