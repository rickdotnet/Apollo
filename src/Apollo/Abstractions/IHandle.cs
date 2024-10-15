namespace Apollo.Abstractions;

public interface IHandle
{
    
}
public interface IHandle<in T> : IHandle where T : ICommand
{
    Task Handle(T message, ApolloContext context, CancellationToken cancellationToken);
}