namespace Apollo.Messaging.Abstractions;

public interface IRequest : IMessage { }
public interface IRequest<T> : IRequest { }