namespace Apollo.Abstractions.Messaging.Commands;

public interface IHandle
{
    
}
public interface IHandle<in T> : IHandle where T : ICommand
{
    Task HandleAsync(T message, CancellationToken cancellationToken);
}