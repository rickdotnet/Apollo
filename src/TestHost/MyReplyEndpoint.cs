using Apollo.Core.Messaging.Requests;
using Microsoft.Extensions.Logging;

namespace TestHost;

public record MyRequest(string Message) : IRequest<bool>;

public class MyReplyEndpoint(ILogger<MyReplyEndpoint> logger) : IReplyTo<MyRequest, bool>
{
    public ValueTask<bool> HandleRequestAsync(MyRequest message, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("MyReplyEndpoint Received: {Message}", message.Message);
        logger.LogInformation("Returning true");
        return ValueTask.FromResult(true);
    }
}