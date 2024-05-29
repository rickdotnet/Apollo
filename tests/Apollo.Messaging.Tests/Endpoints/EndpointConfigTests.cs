using Apollo.Messaging.Endpoints;

namespace Apollo.Messaging.Tests.Endpoints;

public class EndpointConfigTests
{
    [Fact]
    public void SetDurableConsumer_ShouldUpdateDurableConfigProperty()
    {
        var endpointConfig = EndpointConfig.Default;
        endpointConfig.SetDurableConsumer();

        Assert.True(endpointConfig.IsDurableConsumer);
    }
}