using Apollo.Configuration;

namespace Apollo.Providers.NATS.Tests;

// Until a design choice is made on how subjects are handles,
// this test will be the duct tape that holds the code together
public class UtilsTests
{
    [Fact]
    public void GetSubject_ShouldMatch_EndpointNameSet()
    {
        var publishConfig = new PublishConfig
        {
            Namespace = "test.Namespace",
            EndpointName = "test.endpoint"
        };

        var subscriptionConfig = new SubscriptionConfig
        {
            ConsumerName = "",
            Namespace = "test.Namespace",
            EndpointName = "test.endpoint",
            MessageTypes = [],
            IsDurable = false
        };

        var publishSubject = Utils.GetSubject(publishConfig);
        var subscribeSubject = Utils.GetSubject(subscriptionConfig);
        
        const string expectedPublishSubject = "test.Namespace.test.endpoint";
        const string expectedSubscribeSubject = "test.Namespace.test.endpoint.>";
        
        Assert.Equal(expectedPublishSubject, publishSubject);
        Assert.Equal(expectedSubscribeSubject, subscribeSubject);
    }

    [Fact]
    public void GetSubject_ShouldHandle_EndpointNameSet_PrefixWithDollar()
    {
        var publishConfig = new PublishConfig
        {
            Namespace = "$SYS",
            EndpointName = "endpoint"
        };
        
        var subscriptionConfig = new SubscriptionConfig
        {
            ConsumerName = "",
            Namespace = "$SYS",
            EndpointName = "endpoint",
            MessageTypes = [],
            IsDurable = false
        };

        const string expectedPublishSubject = "$SYS.ENDPOINT";
        const string expectedSubscribeSubject = "$SYS.ENDPOINT.>";
        
        var publishSubject = Utils.GetSubject(publishConfig);
        var subscribeSubject = Utils.GetSubject(subscriptionConfig);

        Assert.Equal(expectedPublishSubject, publishSubject);
        Assert.Equal(expectedSubscribeSubject, subscribeSubject);
    }

    [Fact]
    public void GetSubject_ShouldHandle_ExplicitSubject_PrefixWithDollar()
    {
        var publishConfig = new PublishConfig
        {
            EndpointSubject = "$SYS.ENDPOINT.>"
        };
        
        var subscriptionConfig = new SubscriptionConfig
        {
            EndpointSubject = "$SYS.ENDPOINT.>",
            ConsumerName = null!,
            MessageTypes = [],
            IsDurable = false
        };
        
        const string expectedPublishSubject = "$SYS.ENDPOINT";
        const string expectedSubscribeSubject = "$SYS.ENDPOINT.>";
        
        var publishSubject = Utils.GetSubject(publishConfig);
        var subscribeSubject = Utils.GetSubject(subscriptionConfig);
        
        Assert.Equal(expectedPublishSubject, publishSubject);
        Assert.Equal(expectedSubscribeSubject, subscribeSubject);
    }

    [Fact]
    public void GetSubject_ShouldThrow_WhenConfigMissingRequiredFields()
    {
        var publishConfig = new PublishConfig();

        Assert.Throws<ArgumentException>(() => Utils.GetSubject(publishConfig));
    }

    [Fact]
    public void CleanStreamName_ShouldReplaceAndTrimCorrectly()
    {
        const string streamName = "some.stream.name.value.*";

        var cleanedName = streamName.CleanStreamName();

        Assert.Equal("some_stream_name_value", cleanedName);
    }
}