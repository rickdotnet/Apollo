using Apollo.Abstractions;
using Apollo.Configuration;
using NATS.Client.Core;

namespace Apollo.Providers.NATS;

internal class NatsPublisher : IProviderPublisher
{
    private readonly INatsConnection connection;

    public NatsPublisher(INatsConnection connection)
    {
        this.connection = connection;
    }
    public Task PublishAsync(PublishConfig publishConfig, ApolloMessage message, CancellationToken cancellationToken)
    {
        var subject = Utils.GetSubject(publishConfig).TrimEnd('>').TrimEnd('*').TrimEnd('.');
        if(message.MessageType != null)
            subject = $"{subject}.{message.MessageType.Name.ToLower()}";
        
        return connection.PublishAsync($"{subject}", message.Data, cancellationToken: cancellationToken).AsTask();
    }

    public async Task<byte[]> RequestAsync(PublishConfig publishConfig, ApolloMessage message, CancellationToken cancellationToken)
    {
        var subject = Utils.GetSubject(publishConfig).TrimEnd('>').TrimEnd('*').TrimEnd('.');
        if(message.MessageType != null)
            subject = $"{subject}.{message.MessageType.Name.ToLower()}";
        
        var response = await connection.RequestAsync<byte[], byte[]>($"{subject}", message.Data, cancellationToken: cancellationToken).AsTask();
        return response.Data!;
    }
}