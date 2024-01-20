﻿using System.Text;
using System.Text.Json;
using Apollo.Abstractions.Messaging;
using Apollo.Abstractions.Messaging.Commands;
using Apollo.Abstractions.Messaging.Events;
using Apollo.Abstractions.Messaging.Requests;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;

namespace Apollo.Messaging;

public interface IRemotePublisher : IPublisher
{
    string EndpointName { get; }
}

internal class RemotePublisher : IRemotePublisher
{
    public string EndpointName { get; }

    private readonly INatsConnection connection;
    private readonly ILogger<RemotePublisher> logger;


    public RemotePublisher(string endpointName,
        INatsConnection connection,
        ILogger<RemotePublisher> logger)
    {
        EndpointName = endpointName;
        this.connection = connection;
        this.logger = logger;
    }


    public Task SendCommandAsync<TCommand>(TCommand commandMessage, CancellationToken cancellationToken)
        where TCommand : ICommand
    {
        return PublishInternalAsync(commandMessage, cancellationToken);
    }

    // as of now, events and commands are the essentially same
    // and only differ by intent. We might change this in the future
    // to include different headers or something
    public Task BroadcastAsync<TEvent>(TEvent eventMessage, CancellationToken cancellationToken)
        where TEvent : IEvent
    {
        return PublishInternalAsync(eventMessage, cancellationToken);
    }

    private Task PublishInternalAsync<TMessage>(TMessage message, CancellationToken cancellationToken)
    {
        var subject = $"{EndpointName}.{typeof(TMessage).Name}".ToLower();

        logger.LogInformation("Publishing {Name} to {Subject}", typeof(TMessage).Name, subject);

        //var bytes = MessagePackSerializer.Serialize(eventMessage);
        var json = JsonSerializer.Serialize(message);
        var bytes = Encoding.UTF8.GetBytes(json);

        
        return connection.PublishAsync(subject, bytes, opts: new NatsPubOpts { WaitUntilSent = true },
            cancellationToken: cancellationToken).AsTask();
    }

    public async Task<TResponse> SendRequestAsync<TRequest, TResponse>(TRequest requestMessage,
        CancellationToken cancellationToken) where TRequest : IRequest<TResponse>
    {
        var subject = $"{EndpointName}.{typeof(TRequest).Name}".ToLower();

        logger.LogInformation("Publishing {Name} to {Subject}", typeof(TRequest).Name, subject);

        //var bytes = MessagePackSerializer.Serialize(eventMessage);
        var json = JsonSerializer.Serialize(requestMessage);
        var bytes = Encoding.UTF8.GetBytes(json);

        var replyOpts = new NatsSubOpts
        {
            MaxMsgs = 1,
            Timeout = TimeSpan.FromSeconds(30) // TODO: make this configurable
        };

        var result = await connection.RequestAsync<byte[], byte[]>(subject, bytes, replyOpts: replyOpts,
            cancellationToken: cancellationToken);

        var responseJson = Encoding.UTF8.GetString(result.Data);
        logger.LogInformation("Response JSON: {Json}", responseJson);

        var deserialized = JsonSerializer.Deserialize(responseJson, typeof(TResponse));
        return (TResponse)deserialized!;
    }
}