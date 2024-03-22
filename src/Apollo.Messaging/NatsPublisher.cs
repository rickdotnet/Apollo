using System.Text;
using System.Text.Json;
using Apollo.Messaging.Abstractions;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;

namespace Apollo.Messaging;

internal class NatsPublisher : IPublisher
{
    public string Route { get; }
    public bool IsLocalOnly => false;

    private readonly INatsConnection connection;
    private readonly ILogger<NatsPublisher> logger;


    public NatsPublisher(string endpointName,
        INatsConnection connection,
        ILogger<NatsPublisher> logger)
    {
        Route = endpointName;
        this.connection = connection;
        this.logger = logger;
    }

    public Task SendCommandAsync<TCommand>(TCommand message, CancellationToken cancellationToken)
        where TCommand : ICommand
    {
        var subject = $"{Route}.{typeof(TCommand).Name}".ToLower();
        return SendObjectAsync(subject, message, cancellationToken);
    }

    public Task BroadcastAsync<TEvent>(TEvent message, CancellationToken cancellationToken)
        where TEvent : IEvent
    {
        var subject = $"{Route}.{typeof(TEvent).Name}".ToLower();
        return SendObjectAsync(subject, message, cancellationToken);
    }

    public async Task<TResponse?> SendRequestAsync<TRequest, TResponse>(TRequest requestMessage,
        CancellationToken cancellationToken) where TRequest : IRequest<TResponse>
    {
        var subject = $"{Route}.{typeof(TRequest).Name}".ToLower();
        return (TResponse?)await SendRequestAsync(subject, requestMessage, cancellationToken);
    }

    public Task SendObjectAsync(string subject, object commandMessage, CancellationToken cancellationToken)
    {
        //var bytes = MessagePackSerializer.Serialize(eventMessage);
        var json = JsonSerializer.Serialize(commandMessage);
        var bytes = Encoding.UTF8.GetBytes(json);

        return connection.PublishAsync(subject, bytes, opts: new NatsPubOpts { WaitUntilSent = true },
            cancellationToken: cancellationToken).AsTask();
    }

    public async Task<object?> SendRequestAsync(string subject, object requestMessage, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(requestMessage);
        var bytes = Encoding.UTF8.GetBytes(json);

        var replyOpts = new NatsSubOpts
        {
            MaxMsgs = 1,
            Timeout = TimeSpan.FromSeconds(30) // TODO: make this configurable
        };

        var result = await connection.RequestAsync<byte[], byte[]>(subject, bytes, replyOpts: replyOpts,
            cancellationToken: cancellationToken);

        if(result.Data == null)
        {
            logger.LogWarning("Null Response ({Subject})", subject);
            return null;
        }
        var responseJson = Encoding.UTF8.GetString(result.Data);
        logger.LogInformation("Response JSON: {Json}", responseJson);

        var deserialized = JsonSerializer.Deserialize(responseJson, typeof(object));
        return Task.FromResult(deserialized);
    }
}