using Apollo.Messaging;
using Apollo.Messaging.Abstractions;
using Apollo.Messaging.Contracts;
using Microsoft.Extensions.Logging;

namespace TestHost;

public record TestCommand : ICommand
{
    public TestCommand(string Message)
    {
        this.Message = Message;
    }

    public string Message { get; init; }

    public void Deconstruct(out string Message)
    {
        Message = this.Message;
    }
}

public class MyEndpoint : IListenFor<TestEvent>, IHandle<TestCommand>
{
    private readonly ILogger<MyEndpoint> logger;
    private readonly IPublisher localPublisher;
    private readonly IPublisher localRequestPublisher;

    public MyEndpoint(ILogger<MyEndpoint> logger, IPublisherFactory publisherFactory)
    {
        this.logger = logger;
        this.localPublisher = publisherFactory.CreatePublisher(nameof(MyOtherEndpoint), PublisherType.Local);
        this.localRequestPublisher = publisherFactory.CreatePublisher(nameof(MyReplyEndpoint), PublisherType.Local);
    }

    public Task HandleAsync(TestEvent message, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("MyEndpoint Received TestEvent: {Message}", message.Message);
        //localPublisher.BroadcastAsync(new TestEvent("Hello from MyEndpoint"), cancellationToken);
        
        var reply = localRequestPublisher.SendRequestAsync<MyRequest, bool>(new MyRequest("MyEndpoint Request"), cancellationToken);
        logger.LogInformation("MyEndpoint Received Reply: {Reply}", reply.Result);
        return Task.CompletedTask;
    }

    public Task HandleAsync(TestCommand message, CancellationToken cancellationToken)
    {
        logger.LogInformation("MyEndpoint Received: {Message}", message.Message);
        return Task.CompletedTask;
    }
}