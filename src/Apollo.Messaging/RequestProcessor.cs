using System.Threading.Channels;
using Apollo.Messaging.Middleware;
using Apollo.Nats;
using Microsoft.Extensions.DependencyInjection;

namespace Apollo.Messaging;

public class RequestProcessor
{
    private readonly Channel<NatsMessage> channel;
    
    /// <summary>
    /// Singleton ServiceProvider
    /// </summary>
    private readonly IServiceProvider serviceProvider;

    public RequestProcessor(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
        channel = Channel.CreateUnbounded<NatsMessage>();
    }

    public async Task EnqueueMessageAsync(NatsMessage message, CancellationToken cancellationToken)
    {
        // This method is called by external services to enqueue messages
        await channel.Writer.WriteAsync(message, cancellationToken);
    }

    public async Task StartProcessingAsync(int workerCount, Func<IServiceProvider, NatsMessage, CancellationToken, Task> finalHandler, CancellationToken cancellationToken)
    {
        // Start the specified number of worker tasks
        var workers = Enumerable.Range(0, workerCount)
            .Select(_ => ProcessMessagesAsync(finalHandler, cancellationToken));

        // Run all workers
        await Task.WhenAll(workers);
    }

    private async Task ProcessMessagesAsync(Func<IServiceProvider, NatsMessage, CancellationToken, Task> finalHandler, CancellationToken cancellationToken)
    {
        await foreach (var message in channel.Reader.ReadAllAsync(cancellationToken))
        {
            // each message gets it's own pipeline
            using var scope = serviceProvider.CreateScope();
            var scopedMiddlewareExecutor = scope.ServiceProvider.GetRequiredService<MiddlewareExecutor>();
            await scopedMiddlewareExecutor.ExecuteAsync(message, finalHandler, cancellationToken);
        }
    }
}