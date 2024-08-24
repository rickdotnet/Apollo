using System.Reflection;
using System.Text.Json;
using Apollo.Abstractions;
using Apollo.Configuration;

namespace Apollo.Internal;

// TLDR: need two config values to determine
// 1. sync/async mode for message concurrency
// 2. singleton vs transient endpoint instance for each message

// the timing and message pipeline of the endpoint is TBD
// one thought is to send sync/async mode as a config option
// and use a channel to control the flow of messages
// NATS should make that easy since it already has a message queue
// and our handlers will be on the same subscription
// ASB, might be a different story

// another consideration is around the scope of each message
// in sync mode the endpoint might want to track state and thus
// would be processed one message at a time
// in async mode the caller might want to process messages in parallel
// and thus the endpoint instance be new for each message 

internal class SynchronousEndpoint : IApolloEndpoint
{
    private readonly EndpointConfig endpointConfig;
    private readonly ISubscriptionProvider subscriptionProvider;
    private readonly IEndpointProvider? endpointProvider;
    private readonly Type? endpointType;
    private readonly object? endpointInstance;
    private readonly Func<ApolloContext, CancellationToken, Task>? handler;
    private readonly bool handlerOnly = true;
    private Task? endpointTask;
    //private CancellationToken? endpointCancellationToken;

    // subject -> `Handle(message)` cache
    private readonly Dictionary<Type, MethodInfo> handlers = new();

    public SynchronousEndpoint(EndpointConfig endpointConfig,
        Func<ApolloContext, CancellationToken, Task>? handler = null
    )
    {
        subscriptionProvider = endpointConfig.SubscriptionProvider
                               ?? throw new ArgumentException("Subscription provider is required");

        this.endpointConfig = endpointConfig;
        this.handler = handler;

        endpointProvider ??= endpointConfig.EndpointProvider;
        endpointType = endpointConfig.EndpointType;

        if (endpointType is not null)
        {
            if (handler is not null)
                throw new ArgumentException("Cannot have both an endpoint type and a handler"); // or can we?

            if (endpointProvider is null)
                throw new ArgumentException("And endpoint provider is required when creating non-handler Endpoints");

            handlerOnly = false;

            // grab the instance from the DI container 
            endpointInstance = endpointProvider.GetService(endpointType);
        }
        else if (handler is null)
        {
            throw new ArgumentException("Must have either an endpoint type or a handler");
        }
    }

    // start the endpoint
    public Task StartEndpoint(CancellationToken cancellationToken)
    {
        var subscriptionConfig = SubscriptionConfig.ForEndpoint(endpointConfig, endpointType!);

        // create the subscription
        var sub = subscriptionProvider.AddSubscription(subscriptionConfig, InternalHandle);

        // track the task for use in the future
        // prob want to track the sub and control
        // it via the subscription interface
        endpointTask = sub.SubscribeAsync(cancellationToken);

        // let the caller go do other things
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        // sub.StopAsync()
        return ValueTask.CompletedTask;
    }

    private async Task InternalHandle(ApolloContext context, CancellationToken cancellationToken)
    {
        if (handlerOnly)
        {
            await handler!(context, cancellationToken);
            return;
        }

        if (context.Message.MessageType is null)
            throw new InvalidOperationException("Message type not found"); // assume byte[]?

        // get or set cache
        var handleMethod = handlers.GetValueOrDefault(context.Message.MessageType);
        if (handleMethod is null)
        {
            handleMethod = endpointType!.GetMethod("HandleAsync",
                [context.Message.MessageType!, typeof(CancellationToken)]);

            if (handleMethod is null)
                throw new InvalidOperationException("HandleAsync method not found");

            handlers.TryAdd(context.Message.MessageType, handleMethod);
        }

        // invoke the method
        var stringData = System.Text.Encoding.UTF8.GetString(context.Message.Data!);
        var messageObject = JsonSerializer.Deserialize(stringData, context.Message.MessageType!);

        var isRequest = context.Message.MessageType.IsRequest();
        if (isRequest)
        {
            if (!context.ReplyAvailable)
                throw new InvalidOperationException("Uh, oh: No reply available");

            var response =
                await (dynamic)handleMethod.Invoke(endpointInstance, [messageObject, cancellationToken])!;
            
            // TODO: serialization point
            var responseJson = JsonSerializer.Serialize(response);
            var responseBytes = System.Text.Encoding.UTF8.GetBytes(responseJson);
            await context.ReplyAsync(responseBytes, cancellationToken);
        }
        else
        {
            var result = (Task)handleMethod.Invoke(endpointInstance, [messageObject, cancellationToken])!;
            await result;
        }
    }

    private Task Reply(object message, CancellationToken cancellationToken)
    {
        // instance.Handle((type)context.Message);
        throw new NotImplementedException();
    }
}