using Apollo.Nats;

namespace Apollo.Messaging.Middleware;

public interface IMessageMiddleware
{
    Task InvokeAsync(NatsMessage message, Func<Task> next, CancellationToken cancellationToken);
}

public class MiddlewareExecutor
{
    private readonly IEnumerable<IMessageMiddleware> middleware;
    private readonly IServiceProvider serviceProvider;

    public MiddlewareExecutor(IEnumerable<IMessageMiddleware> middleware, IServiceProvider serviceProvider)
    {
        this.middleware = middleware ?? throw new ArgumentNullException(nameof(middleware));
        this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public async Task ExecuteAsync(NatsMessage message, Func<IServiceProvider, NatsMessage, CancellationToken, Task> finalHandler,
        CancellationToken cancellationToken)
    {
        var next = () => finalHandler(serviceProvider, message, cancellationToken);
        foreach (var middleMan in middleware.Reverse())
        {
            var currentNext = next;
            next = () => middleMan.InvokeAsync(message, currentNext, cancellationToken);
        }

        await next();
    }
}