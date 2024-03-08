using Apollo.Abstractions.Messaging.Events;

namespace Apollo.Messaging.Contracts;

public record TestEvent(string Message) : IEvent;