using Apollo.Messaging.Abstractions;

namespace Apollo.Messaging.Contracts;

public record TestEvent(string Message) : IEvent;