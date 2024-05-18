namespace Apollo.Messaging.Abstractions;

public interface IRemotePublisherFactory
{
    IRemotePublisher CreatePublisher(string route);
}