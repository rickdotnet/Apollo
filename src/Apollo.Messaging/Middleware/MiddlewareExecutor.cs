namespace Apollo.Messaging.Middleware;

public interface IMessageMiddleware
{
    Task InvokeAsync(MessageContext message, Func<Task> next, CancellationToken cancellationToken);
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

    public async Task ExecuteAsync(MessageContext messageContext, Func<IServiceProvider, MessageContext, CancellationToken, Task>? finalHandler = null, CancellationToken cancellationToken = default)
    {
        // provide a default final handler if none is given
        finalHandler ??= (_, _, _) => Task.CompletedTask;

        var next = () => finalHandler(serviceProvider, messageContext, cancellationToken);

        // execute the middleware in reverse order, so the first middleware in the list is the outermost one
        foreach (var middlewareComponent in middleware.Reverse())
        {
            var currentNext = next;
            next = () => middlewareComponent.InvokeAsync(messageContext, currentNext, cancellationToken);
        }

        // start the middleware pipeline execution
        await next();
    }
}