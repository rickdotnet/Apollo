namespace Apollo.Nats;

public interface INatsSubscriber
{
    Task SubscribeAsync(Func<NatsMessage, CancellationToken, Task<bool>> handler);
}