namespace Apollo.Abstractions.Messaging.Commands;

public interface ICommand : IMessage
{
}

// prevent confusion between local and remote?
public interface ILocalCommand : ICommand
{
}