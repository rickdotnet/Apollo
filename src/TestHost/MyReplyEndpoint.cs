using Apollo.Abstractions.Messaging.Requests;
using Microsoft.Extensions.Logging;

namespace TestHost;

public record MyRequest : IRequest<bool>
{
    public MyRequest(string Message)
    {
        this.Message = Message;
    }

    public string Message { get; init; }

    public void Deconstruct(out string Message)
    {
        Message = this.Message;
    }
}

public class MyReplyEndpoint : IReplyTo<MyRequest, bool>
{
    private readonly ILogger<MyReplyEndpoint> logger1;

    public MyReplyEndpoint(ILogger<MyReplyEndpoint> logger)
    {
        logger1 = logger;
    }

    public ValueTask<bool> HandleRequestAsync(MyRequest message, CancellationToken cancellationToken = default)
    {
        logger1.LogInformation("MyReplyEndpoint Received: {Message}", message.Message);
        logger1.LogInformation("Returning true");
        return ValueTask.FromResult(true);
    }
}