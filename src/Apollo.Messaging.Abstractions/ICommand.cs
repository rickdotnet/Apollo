namespace Apollo.Messaging.Abstractions;

public interface ICommand : IMessage
{
}

// prevent confusion between local and remote?
public interface ILocalCommand : ICommand
{
}