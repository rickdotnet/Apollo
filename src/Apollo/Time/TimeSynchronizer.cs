using Apollo.Configuration;
using Microsoft.Extensions.Logging;
using NATS.Client.Core;

namespace Apollo.Time;

internal class TimeSynchronizer
{
    private readonly INatsConnection natsConnection;
    private readonly ApolloConfig config;
    private readonly ILogger<TimeSynchronizer> logger;
    private long timeOffset;
    private Task internalTask;
    private Timer? broadcastTimer;

    // The idea here is that this would be added with something like apolloBuilder.WithTimeSynchronizer()
    // then, you'd add a hosted service that would listen for time messages and update the time offset
    // t
    // whichever server is the time keeper would then instead broadcast the time to all other servers
    // not sure if that belongs here or in a timepublisher class
    public TimeSynchronizer(INatsConnection natsConnection, ApolloConfig config, ILogger<TimeSynchronizer> logger)
    {
        this.natsConnection = natsConnection;
        this.config = config;
        this.logger = logger;

        switch (config.TimeSyncMode)
        {
            case TimeSyncMode.Receive:
                internalTask = SubscribeToTimeMessages(config.TimeSubject);
                break;
            case TimeSyncMode.Broadcast:
                internalTask = Task.CompletedTask;
                // initialize and start the timer to broadcast time messages at a regular interval
                broadcastTimer = new Timer(BroadcastTime, null, TimeSpan.FromSeconds(30), TimeSpan.FromMinutes(15));
                break;
            default:
                internalTask = Task.CompletedTask;
                break;
        }
    }

    private async Task SubscribeToTimeMessages(string timeSubject)
    {
        // cancellationToken needs implemented
        await foreach (var msg in natsConnection.SubscribeAsync<byte[]>(timeSubject))
        {
            logger.LogInformation("Received `time` message");
            if (msg.Data == null)
            {
                logger.LogWarning("Received `time` message with no data");
                continue;
            }

            var tickData = msg.Data!;
            var receivedTime = DateTime.UtcNow;

            if (tickData.Length != 8)
                throw new ArgumentException("Data must be 8 bytes long.");

            var sentTime = new DateTime(BitConverter.ToInt64(tickData, 0), DateTimeKind.Utc);
            var latency = (receivedTime - sentTime).TotalMilliseconds;

            // update the time offset, but... safety first.
            Interlocked.Exchange(ref timeOffset, (long)latency);
        }
    }

    private async void BroadcastTime(object? state)
    {
        try
        {
            var offset = Interlocked.Read(ref timeOffset);
            var currentTime = DateTime.UtcNow.AddMilliseconds(offset);
            var utcTicks = currentTime.ToUniversalTime().Ticks;
            var tickData = BitConverter.GetBytes(utcTicks);

            await natsConnection.PublishAsync(config.TimeSubject, tickData).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error broadcasting time");
        }
    }
}