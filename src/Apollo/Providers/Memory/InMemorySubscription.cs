using System.Threading.Channels;
using Apollo.Abstractions;

namespace Apollo.Providers.Memory;

internal class InMemorySubscription : ISubscription
{
    public ChannelWriter<ApolloContext> Writer { get; }
    private readonly Func<ApolloContext, CancellationToken, Task> handler;
    private readonly Channel<ApolloContext> channel;

    public InMemorySubscription(Func<ApolloContext, CancellationToken, Task> handler)
    {
        this.handler = handler;
        channel = Channel.CreateUnbounded<ApolloContext>();
        Writer = channel.Writer;
    }

    public async Task Subscribe(CancellationToken cancellationToken)
    {
        // channel reader
        await foreach (var context in channel.Reader.ReadAllAsync(cancellationToken))
        {
            await handler(context, cancellationToken);
        }
    }
}