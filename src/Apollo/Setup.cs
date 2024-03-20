using Apollo.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NATS.Client.Core;
using NATS.Client.Hosting;

namespace Apollo;

public static class Setup
{
    public static IServiceCollection AddApollo(this IServiceCollection services)
        => AddApollo(services, ApolloConfig.Default);
    
    public static IServiceCollection AddApollo(this IServiceCollection services, Action<ApolloBuilder> builderAction)
        => AddApollo(services, ApolloConfig.Default, builderAction);

    public static IServiceCollection AddApollo(this IServiceCollection services, ApolloConfig config,
        Action<ApolloBuilder>? builderAction = null)
    {
        services.AddSingleton(config);
        services.AddNats(configureOpts: opts => opts with
        {
            Url = config.Url,
            //LoggerFactory = new FakeFactory(),
            ConnectTimeout = TimeSpan.FromSeconds(10),
            RequestTimeout = TimeSpan.FromSeconds(10),
            AuthOpts = NatsAuthOpts.Default with
            {
                CredsFile = config.CredsFile,
                Token = config.Token,
                NKey = config.NKey,
                Seed = config.Seed,
                Jwt = config.Jwt,
            }
        });

        var builder = new ApolloBuilder(services, config);
        builderAction?.Invoke(builder);

        return services;
    }

    public static ILogger GetLogger<T>(this IServiceProvider serviceProvider)
        => serviceProvider.GetService<ILogger<T>>()
           ?? (ILogger)NullLogger.Instance;
}

public class FakeFactory : ILoggerFactory
{
    public void Dispose()
    {
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new OopsiePoopsie();
    }

    public void AddProvider(ILoggerProvider provider)
    {
    }
}

public class OopsiePoopsie : ILogger
{
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        Console.WriteLine(state);
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    public IDisposable BeginScope<TState>(TState state)
    {
        return new JesusChrist();
    }
}

public class JesusChrist : IDisposable
{
    public void Dispose()
    {
    }
}