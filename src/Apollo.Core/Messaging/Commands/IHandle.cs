namespace Apollo.Core.Messaging.Commands;

public interface IHandle
{
    
}
public interface IHandle<in T> : IHandle where T : ICommand
{
    ValueTask HandleCommandAsync(T message, CancellationToken cancellationToken);
}