using Apollo.Configuration;
using Apollo.Time;
using Microsoft.Extensions.DependencyInjection;

namespace Apollo;

public class ApolloBuilder
{
    public IServiceCollection Services { get; }
    public ApolloConfig Config { get; }

    //private readonly IEndpointBuilder endpointBuilder;

    public ApolloBuilder(IServiceCollection services, ApolloConfig config)
    {
        Services = services;
        Config = config;
    }
   
    public void WithService(Action<IServiceCollection> action)
    {
        action(Services);
    }
    
    public void WithTimeSynchronizer()
    {
        Services.AddSingleton<TimeSynchronizer>();
        // add hosted service
    }
}

