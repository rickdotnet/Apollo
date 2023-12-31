﻿namespace Apollo.Core.Nats;

public interface INatsSubscriber
{
    Task SubscribeAsync(Func<NatsMessageReceivedEvent, CancellationToken, Task<bool>> handler);
}