using Apollo.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Apollo;

public class ApolloBuilder
{
    public IServiceCollection Services { get; }
    public ApolloConfig Config { get; }

    public ApolloBuilder(IServiceCollection services, ApolloConfig config)
    {
        Services = services;
        Config = config;
    }
   
    public void WithService(Action<IServiceCollection> action) 
        => action(Services);
}

