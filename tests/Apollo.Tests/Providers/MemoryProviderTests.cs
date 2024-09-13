using Apollo.Abstractions;
using Apollo.Configuration;
using Apollo.Providers.Memory;
using FakeItEasy;
using Xunit.Sdk;

namespace Apollo.Tests.Providers;

public class MemoryProviderTests
{
    private readonly PublishConfig publishConfig;
    private readonly SubscriptionConfig subscriptionConfig;

    public MemoryProviderTests()
    {
        var endpointConfig = new EndpointConfig
        {
            Namespace = "test",
            EndpointName = "endpoint",
            ConsumerName = "",
            IsDurable = false,
            CreateMissingResources = false
        };
        subscriptionConfig = SubscriptionConfig.ForEndpoint(endpointConfig);
        publishConfig = endpointConfig.ToPublishConfig();
    }
    
    [Fact]
    public async Task PublishAsync_ShouldPublishMessage()
    {
        var provider = new InMemoryProvider();
        
        var handler = A.Fake<Func<ApolloContext, CancellationToken, Task>>();

        var sub = provider.AddSubscription(subscriptionConfig, handler);
        _ = sub.Subscribe(CancellationToken.None);
        
        await Task.Delay(500);

        var message = new ApolloMessage { MessageType = typeof(TestMessage) };
        await provider.Publish(publishConfig, message, CancellationToken.None);
        await Task.Delay(500);
        
        // Assert
        A.CallTo(() => handler(A<ApolloContext>._, A<CancellationToken>._)).MustHaveHappened();
    }

    [Fact]
    public async Task RequestAsync_ShouldReturnResponse()
    {
        var provider = new InMemoryProvider();
        var sub = provider.AddSubscription(subscriptionConfig, async (context, token) =>
        {
            if (context.ReplyAvailable)
            {
                await context.Reply([1, 2, 3], token);
            }
        });
        
        _ = sub.Subscribe(CancellationToken.None);
        await Task.Delay(500);

        // timeout to prevent jammed reply
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        
        var message = new ApolloMessage { MessageType = typeof(TestRequest) };
        var response = await provider.Request(publishConfig, message, cts.Token);

        Assert.Equal([1, 2, 3 ], response);
    }
    
    [Fact]
    public async Task MultipleHandlers_ShouldReceiveMessages()
    {
        var provider = new InMemoryProvider();
        var handler1 = A.Fake<Func<ApolloContext, CancellationToken, Task>>();
        var handler2 = A.Fake<Func<ApolloContext, CancellationToken, Task>>();

        var sub1 = provider.AddSubscription(subscriptionConfig, handler1);
        var sub2 = provider.AddSubscription(subscriptionConfig, handler2);

        _ = sub1.Subscribe(CancellationToken.None);
        _ = sub2.Subscribe(CancellationToken.None);
        await Task.Delay(500);

        var message = new ApolloMessage { MessageType = typeof(TestMessage) };
        await provider.Publish(publishConfig, message, CancellationToken.None);
        await Task.Delay(500);
        
        // Asserting both handlers received the message
        A.CallTo(() => handler1(A<ApolloContext>._, A<CancellationToken>._)).MustHaveHappened();
        A.CallTo(() => handler2(A<ApolloContext>._, A<CancellationToken>._)).MustHaveHappened();
    }

    [Fact]
    public async Task HandlerException_ShouldNotCrashSystem()
    {
        var provider = new InMemoryProvider();
        var handler = A.Fake<Func<ApolloContext, CancellationToken, Task>>();

        var sub = provider.AddSubscription(subscriptionConfig, (_, _) =>
        {
            throw new Exception("Test handler exception");
        });

        _ = sub.Subscribe(CancellationToken.None);
        await Task.Delay(500);

        var message = new ApolloMessage { MessageType = typeof(TestMessage) };
        await provider.Publish(publishConfig, message, CancellationToken.None);
        await Task.Delay(500);

        // Assert: No crash, handled the exception internally
        A.CallTo(() => handler(A<ApolloContext>._, A<CancellationToken>._)).MustNotHaveHappened();
    }

    private class TestRequest : IRequest<TestResponse> { }
    private class TestResponse { }
}