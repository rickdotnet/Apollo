using System.Text;
using System.Text.Json;
using NATS.Client.Core;

namespace Apollo.Messaging.Replier;

public class NatsReplier : IReplier
{
    private readonly INatsConnection connection;
    private readonly string replyTo;
    public NatsReplier(INatsConnection connection, string replyTo)
    {
        this.connection = connection;
        this.replyTo = replyTo;
    }

    public Task ReplyAsync(object response, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(response);
        var bytes = Encoding.UTF8.GetBytes(json);
        return connection.PublishAsync(replyTo, bytes, cancellationToken: cancellationToken).AsTask();
    }
}