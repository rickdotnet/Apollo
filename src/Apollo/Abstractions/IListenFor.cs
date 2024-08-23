namespace Apollo.Abstractions;

public interface IListenFor
{
    
}
public interface IListenFor<in TEvent> : IListenFor where TEvent : IEvent
{
    public Task HandleAsync(TEvent message, CancellationToken cancellationToken = default);
}
