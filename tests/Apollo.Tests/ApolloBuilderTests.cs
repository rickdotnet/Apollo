using Apollo.Configuration;
using FakeItEasy;
using Microsoft.Extensions.DependencyInjection;

namespace Apollo.Tests;

public class ApolloBuilderTests
{
    private readonly IServiceCollection services;
    private readonly ApolloConfig config;
    private readonly ApolloBuilder apolloBuilder;

    public ApolloBuilderTests()
    {
        services = A.Fake<IServiceCollection>();
        config = new ApolloConfig(); // Assuming you have a default constructor
        apolloBuilder = new ApolloBuilder(services, config);
    }

    [Fact]
    public void Constructor_ShouldInitializeProperties()
    {
        Assert.Same(services, apolloBuilder.Services);
        Assert.Same(config, apolloBuilder.Config);
    }

    [Fact]
    public void WithService_ShouldExecuteActionOnServices()
    {
        var actionInvoked = false;
        Action<IServiceCollection> action = _ => actionInvoked = true;

        apolloBuilder.WithService(action);

        Assert.True(actionInvoked);
    }
}