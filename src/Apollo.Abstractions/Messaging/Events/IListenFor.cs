namespace Apollo.Abstractions.Messaging.Events;

public interface IListenFor
{
    
}
public interface IListenFor<in TEvent> : IListenFor where TEvent : IEvent
{
    public Task HandleEventAsync(TEvent message, CancellationToken cancellationToken = default);
}