namespace Apollo.Core.Messaging.Requests;

public interface IRequest : IMessage { }
public interface IRequest<T> : IRequest { }