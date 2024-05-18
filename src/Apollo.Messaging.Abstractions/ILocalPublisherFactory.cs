namespace Apollo.Messaging.Abstractions;

public interface ILocalPublisherFactory 
{
    ILocalPublisher CreatePublisher(string route);
}