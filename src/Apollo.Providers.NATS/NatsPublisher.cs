using Apollo.Abstractions;
using Apollo.Configuration;
using Microsoft.Extensions.Primitives;
using NATS.Client.Core;

namespace Apollo.Providers.NATS;

internal class NatsPublisher : IProviderPublisher
{
    private readonly INatsConnection connection;

    public NatsPublisher(INatsConnection connection)
    {
        this.connection = connection;
    }

    public Task Publish(PublishConfig publishConfig, ApolloMessage message, CancellationToken cancellationToken)
    {
        var subject = DefaultSubjectTypeMapper.From(publishConfig).EndpointSubject;

        return connection.PublishAsync(
            $"{subject}",
            message.Data,
            headers: new NatsHeaders((Dictionary<string, StringValues>)message.Headers),
            cancellationToken: cancellationToken).AsTask();
    }

    public async Task<byte[]> Request(
        PublishConfig publishConfig,
        ApolloMessage message,
        CancellationToken cancellationToken)
    {
        var subject = DefaultSubjectTypeMapper.From(publishConfig).EndpointSubject;

        var response = await connection
            .RequestAsync<byte[], byte[]>(
                $"{subject}",
                message.Data,
                headers: new NatsHeaders((Dictionary<string, StringValues>)message.Headers),
                cancellationToken: cancellationToken).AsTask();
        return response.Data!;
    }
}