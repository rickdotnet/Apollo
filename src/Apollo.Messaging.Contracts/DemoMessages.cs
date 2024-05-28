using Apollo.Messaging.Abstractions;

namespace Apollo.Messaging.Contracts;

public record DemoMessages(string Message) : IEvent;
public record TestCommand(string Message) : ICommand;
public record MyRequest(string Message) : IRequest<bool>;