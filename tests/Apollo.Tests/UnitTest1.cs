using System.Text;
using Apollo.Abstractions;
using Apollo.Configuration;
using Apollo.Providers.Memory;
using FakeItEasy;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Apollo.Tests;

public class ApolloClientTests
{
    private readonly ApolloClient client;
    private readonly EndpointConfig endpointConfig;
    private readonly PublishConfig publishConfig;
    private readonly ISubscriptionProvider subscriptionProvider = new InMemoryProvider();

    public ApolloClientTests()
    {
        var apolloConfig = new ApolloConfig
        {
            DefaultNamespace = "test",
            InstanceId = "instance-1"
        };

        client = new ApolloClient(apolloConfig, subscriptionProvider);
        endpointConfig = new EndpointConfig
        {
            Namespace = "test",
            EndpointName = "endpoint",
            ConsumerName = "consumer",
            IsDurable = false,
            CreateMissingResources = false
        };
        publishConfig = endpointConfig.ToPublishConfig();
    }

    [Fact]
    public async Task ShouldPublishAndHandleMessage()
    {
        var handler = A.Fake<Func<ApolloContext, CancellationToken, Task>>();
        var endpoint = client.AddHandler(endpointConfig, handler);

        _ = endpoint.StartEndpoint(CancellationToken.None);
        await Task.Delay(500); // Ensure subscription is set up

        var publisher = client.CreatePublisher(publishConfig);
        await publisher.BroadcastAsync(new TestMessage(), CancellationToken.None);

        await Task.Delay(1000);
        
        A.CallTo(() => handler(A<ApolloContext>._, A<CancellationToken>._)).MustHaveHappened();
    }

    [Fact]
    public async Task ShouldHandleRequestResponse()
    {
        var apolloConfig = new ApolloConfig
        {
            DefaultNamespace = "test",
            InstanceId = "instance-1"
        };
        //var client = new ApolloClient(apolloConfig, subscriptionProvider);
        var endpoint = client.AddHandler(endpointConfig, async (context, token) =>
        {
            if (context.ReplyAvailable)
            {
                var bytes = JsonSerializer.SerializeToUtf8Bytes(new TestResponse("Response"));
                await context.ReplyAsync(bytes, token);
            }
        });

        _ = endpoint.StartEndpoint(CancellationToken.None);
        await Task.Delay(500); // Ensure subscription is set up

        var publisher = client.CreatePublisher(publishConfig);
        var response = await publisher.RequestAsync<TestRequest,TestResponse>(new TestRequest("Request"), CancellationToken.None);

        Assert.Equal(new TestResponse("Response"), response);
    }
    
    private record TestRequest(string Message) : IRequest<TestResponse>;
    private record TestResponse(string Message);
    private record TestMessage : IEvent;
}