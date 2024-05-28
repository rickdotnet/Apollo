using System.Text;
using System.Text.Json;
using Apollo.Messaging.Abstractions;
using Azure.Messaging.ServiceBus;

namespace Apollo.Messaging.Azure;

public class AzureReplier : IReplier
{
    private readonly ServiceBusClient client;
    private readonly string replyTo;
    private readonly string sessionId;
    public AzureReplier(ServiceBusClient client, string replyTo)
    {
        this.client = client;
        
        var lastPeriod = replyTo.LastIndexOf('.');
        this.replyTo = replyTo[..lastPeriod];
        this.sessionId = replyTo[(lastPeriod+1)..];
    }

    public async Task ReplyAsync(object response, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(response);
        var bytes = Encoding.UTF8.GetBytes(json);
        
        var sender = client.CreateSender(replyTo);
        var responseMessage = new ServiceBusMessage(bytes)
        {
            SessionId = sessionId // Set the SessionId to the ReplyToSessionId from the request
        };
        await sender.SendMessageAsync(responseMessage, cancellationToken);
    }
}