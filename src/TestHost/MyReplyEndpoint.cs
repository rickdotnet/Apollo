using Apollo.Core.Messaging.Requests;

namespace TestHost;

public record MyRequest(string Message) : IRequest<bool>;

public class MyReplyEndpoint : IReplyTo<MyRequest, bool>
{
    public ValueTask<bool> HandleRequestAsync(MyRequest message, CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(true);
    }
}