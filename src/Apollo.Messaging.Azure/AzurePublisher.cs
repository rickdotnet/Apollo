using System.Text;
using System.Text.Json;
using Apollo.Messaging.Abstractions;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.Extensions.Logging;

namespace Apollo.Messaging.Azure;

public class AzurePublisher : IRemotePublisher
{
    public string Route { get; }
    public bool IsLocalOnly => false;

    private readonly ServiceBusClient client;
    private readonly ServiceBusAdministrationClient adminClient;
    private readonly ILogger logger;

    public AzurePublisher(string route, ServiceBusClient client, ServiceBusAdministrationClient adminClient,
        ILogger logger)
    {
        Route = route;
        this.client = client;
        this.adminClient = adminClient;
        this.logger = logger;
    }

    public async Task SendCommandAsync<TCommand>(TCommand commandMessage, CancellationToken cancellationToken)
        where TCommand : ICommand
    {
        var subject = $"{Route}.{typeof(TCommand).Name}".ToLower();
        await SendObjectAsync(subject, commandMessage, cancellationToken);
    }

    public async Task BroadcastAsync<TEvent>(TEvent eventMessage, CancellationToken cancellationToken)
        where TEvent : IEvent
    {
        var subject = $"{Route}.{typeof(TEvent).Name}".ToLower();
        await SendObjectAsync(subject, eventMessage, cancellationToken);
    }

    public async Task SendObjectAsync(string subject, object commandMessage, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(commandMessage);
        var bytes = Encoding.UTF8.GetBytes(json);

        var message = new ServiceBusMessage(bytes)
        {
            MessageId = Guid.NewGuid().ToString(),
            ContentType = "application/json"
        };

        message.SetSubject(subject);

        var sender = client.CreateSender(message.Subject);
        try
        {
            await sender.SendMessageAsync(message, cancellationToken);
        }
        finally
        {
            await sender.DisposeAsync();
        }
    }

    public async Task<TResponse?> SendRequestAsync<TRequest, TResponse>(TRequest requestMessage,
        CancellationToken cancellationToken) where TRequest : IRequest<TResponse>
    {
        var subject = $"{Route}.{typeof(TRequest).Name}".ToLower();
        return await SendRequestAsync<TResponse>(subject, requestMessage, cancellationToken);
    }

    public Task<object?> SendRequestAsync(string subject, object requestMessage, CancellationToken cancellationToken)
        => SendRequestAsync<object>(subject, requestMessage, cancellationToken);

    public async Task<TResponse?> SendRequestAsync<TResponse>(string subject, object requestMessageRaw,
        CancellationToken cancellationToken)
    {
        // TODO: configure this
        var replyTopicName = "apollo.requestline";

        // unique per app instance
        var subscriptionName = "replies-dc1cfd6c-29bb-4967-95e9-f8186de9460b";

        var json = JsonSerializer.Serialize(requestMessageRaw);
        var bytes = Encoding.UTF8.GetBytes(json);

        // generated at request time
        var sessionId = "ca9252e8-9aeb-4e6c-b876-f49838c162dc";

        var requestMessage = new ServiceBusMessage(bytes)
        {
            MessageId = Guid.NewGuid().ToString(),
            ContentType = "application/json",
            ReplyTo = replyTopicName,
            ReplyToSessionId = sessionId,
        };

        requestMessage.SetSubject(subject);

        var receiver = await client.AcceptSessionAsync(replyTopicName, subscriptionName, sessionId,
            cancellationToken: cancellationToken);

        var originalSender = client.CreateSender(requestMessage.Subject);
        try
        {
            await originalSender.SendMessageAsync(requestMessage, cancellationToken);

            var responseMessage = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(15), cancellationToken);
            if (responseMessage != null && responseMessage.MessageId == requestMessage.MessageId)
                await receiver.CompleteMessageAsync(responseMessage, cancellationToken);

            if (responseMessage?.Body == null)
                return default;

            var deserialized = JsonSerializer.Deserialize<TResponse>(responseMessage.Body.ToString());
            return deserialized;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending request");
            return default;
        }
        finally
        {
            await receiver.DisposeAsync();
            await originalSender.DisposeAsync();
        }
    }
}

public static class ServiceBusMessageExtensions
{
    /// <summary>
    /// Apollo uses the subject to determine the message type. ASB listens
    /// at the endpoint level, but we publish at the message type level
    /// the ASB publisher splits the subject into the ASB subject and the
    /// message type then reconstructs the subject on the receiver side
    /// </summary>
    /// <param name="message"></param>
    /// <param name="subject"></param>
    public static void SetSubject(this ServiceBusMessage message, string subject)
    {
        var lastPeriod = subject.LastIndexOf('.');
        var asbSubject = subject[..lastPeriod];
        var messageType = subject[(lastPeriod + 1)..];
        message.Subject = asbSubject;
        message.ApplicationProperties.Add("message-type", messageType);
    }

    public static string? GetMessageType(this ServiceBusReceivedMessage message)
    {
        if (message.ApplicationProperties.TryGetValue("message-type", out var messageType))
            return messageType as string;
        return null;
    }
}