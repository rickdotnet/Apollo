﻿using Microsoft.Extensions.Primitives;
using NATS.Client.Core;

namespace Apollo.Nats;

public class NatsMessage
{
    // if we expose NatsMessageReceivedEvent to the outside world,
    // we should keep this internal
    internal NatsSubscriptionConfig Config { get; init; }
    public string Subject { get; init; } = string.Empty;
    public IDictionary<string, StringValues>? Headers { get; init; }
    public object? Message { get; init; }
    public string? ReplyTo { get; init; }
    public INatsConnection? Connection { get; init; }
}