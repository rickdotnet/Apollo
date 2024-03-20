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
        logger.LogInformation("Subject: {Subject}", Context.Subject);
        logger.LogInformation("Source: {Source}", Context.Source);
        logger.LogInformation("ReplyTo: {ReplyTo}", Context.ReplyTo);
        Context.Headers.ToList().ForEach(x => logger.LogInformation("Header: {Key}={Value}", x.Key, x.Value));
        logger.LogInformation("Returning true");
        return Task.FromResult(true);
    }
}