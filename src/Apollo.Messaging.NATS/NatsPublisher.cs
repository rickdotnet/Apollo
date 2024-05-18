using System.Text;
using System.Text.Json;
using Apollo.Messaging.Abstractions;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;

namespace Apollo.Messaging.NATS;

internal class NatsPublisher : IRemotePublisher
{
    public string Route { get; }
    public bool IsLocalOnly => false;

    private readonly INatsConnection connection;
    private readonly ILogger logger;

    public NatsPublisher(
        string endpointName,
        INatsConnection connection,
        ILogger logger)
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
    
    public Task SendObjectAsync(string subject, object message, CancellationToken cancellationToken)
    {
        //var bytes = MessagePackSerializer.Serialize(eventMessage);
        var json = JsonSerializer.Serialize(message);
        var bytes = Encoding.UTF8.GetBytes(json);

        // TODO: start landing on header format
        //       ideally the same path for local and remote
        //       with different config
        // ex: Ids should be added consistently
        // var msgId = idGenerator.CreateId().ToString();
        // var headers = new NatsHeaders
        // {
        //     { "Message-Id", msgId },
        //     { "Nats-Msg-Id", msgId } // do we want to set this?
        // };
        return connection.PublishAsync(subject, bytes, headers: null, opts: new NatsPubOpts {   },
            cancellationToken: cancellationToken).AsTask();
    }

    public async Task<TResponse?> SendRequestAsync<TRequest, TResponse>(TRequest requestMessage,
        CancellationToken cancellationToken) where TRequest : IRequest<TResponse>
    {
        var subject = $"{Route}.{typeof(TRequest).Name}".ToLower();
        return await SendRequestAsync<TResponse>(subject, requestMessage, cancellationToken);
    }
    public Task<object?> SendRequestAsync(string subject, object requestMessage, CancellationToken cancellationToken) 
        => SendRequestAsync<object>(subject, requestMessage, cancellationToken);

    public async Task<TResponse?> SendRequestAsync<TResponse>(string subject, object requestMessage, CancellationToken cancellationToken)
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
            return default;
        }
        var responseJson = Encoding.UTF8.GetString(result.Data);
        logger.LogInformation("Response JSON2: {Json}", responseJson);

        var deserialized = JsonSerializer.Deserialize<TResponse>(responseJson);
        return deserialized;
    }
}