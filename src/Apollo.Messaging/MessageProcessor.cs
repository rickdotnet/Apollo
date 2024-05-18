using System.Threading.Channels;
using Apollo.Messaging.Abstractions;
using Apollo.Messaging.Middleware;
using Apollo.Messaging.Replier;
using Microsoft.Extensions.DependencyInjection;

namespace Apollo.Messaging;

public class MessageProcessor
{
    private readonly Channel<MessageContext> channel;
    
    /// <summary>
    /// Singleton ServiceProvider
    /// </summary>
    private readonly IServiceProvider serviceProvider;

    public MessageProcessor(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
        channel = Channel.CreateUnbounded<MessageContext>();
    }

    public ValueTask EnqueueMessageAsync(MessageContext messageContext, CancellationToken cancellationToken) 
        => channel.Writer.WriteAsync(messageContext, cancellationToken);

    public Task StartProcessingAsync(int workerCount, Func<IServiceProvider, MessageContext, CancellationToken, Task> finalHandler, CancellationToken cancellationToken)
    {
        // start the specified number of worker tasks
        var workers = 
            Enumerable.Range(0, workerCount).Select(_ => ProcessMessagesAsync(finalHandler, cancellationToken));

        // run all workers
        return Task.WhenAll(workers);
    }

    private async Task ProcessMessagesAsync(Func<IServiceProvider, MessageContext, CancellationToken, Task> finalHandler, CancellationToken cancellationToken)
    {
        await foreach (var message in channel.Reader.ReadAllAsync(cancellationToken))
        {
            // each message gets it's own pipeline/execution scope
            using var scope = serviceProvider.CreateScope();
            var pipelineMessage = message;
            
            if (!string.IsNullOrEmpty(message.ReplyTo)
                && message.Replier is NoOpReplier
                && message.ReplierType != null)
            {
                pipelineMessage = message with
                {
                    Replier = (IReplier)scope.ServiceProvider.GetRequiredService(message.ReplierType)
                };
            }
            
            var scopedMiddlewareExecutor = scope.ServiceProvider.GetRequiredService<MiddlewareExecutor>();
            await scopedMiddlewareExecutor.ExecuteAsync(pipelineMessage, finalHandler, cancellationToken);
        }
    }
    public async Task<object?> ProcessLocalMessageAsync(MessageContext messageContext, CancellationToken cancellationToken)
    {
        var isRequest = messageContext.Message?.GetType().IsRequest() == true;
        
        IReplier replier = isRequest ? new LocalReplier() : NoOpReplier.Instance;
        messageContext = messageContext with { Replier = replier };

        using var scope = serviceProvider.CreateScope();
        var scopedMiddlewareExecutor = scope.ServiceProvider.GetRequiredService<MiddlewareExecutor>();
        await scopedMiddlewareExecutor.ExecuteAsync(messageContext,null, cancellationToken);

        // Wait for the response
        if (!isRequest) return null;
        
        // TODO: need to timeout here in case of failure/no response
        var response = await ((LocalReplier)replier).ResponseTask;
        return response;
    }
}