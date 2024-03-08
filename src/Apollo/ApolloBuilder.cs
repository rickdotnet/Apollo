using Apollo.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Apollo;

public class ApolloBuilder
{
    public IServiceCollection Services { get; }
    public ApolloConfig Config { get; }

    //private readonly IEndpointBuilder endpointBuilder;

    public ApolloBuilder(IServiceCollection services, ApolloConfig config)
    {
        this.Services = services;
        this.Config = config;
        //endpointBuilder = new EndpointBuilder(services, config);
    }
   
    public void WithService(Action<IServiceCollection> action)
    {
        action(Services);
    }
}

