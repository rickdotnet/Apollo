using Apollo.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NATS.Client.Core;

namespace Apollo.Messaging.Time;

// The idea here is that this would be added with something like apolloBuilder.WithTimeSynchronizer()
// then, you'd listen for time messages and update the time offset
// 
// whichever server(s) is/are the time keeper could occasionally broadcast the time to all other servers
// update the offset based on the latency, and then use that offset to calculate the current time
//
// once a time offset is set, it should be updated periodically to account for drift
// need to determine the frequency of updates and run some tests to see how much drift we get
internal class TimeSynchronizer
{
    private readonly INatsConnection natsConnection;
    private readonly ApolloIdGenTimeSource? timeSource;
    private readonly ILogger<TimeSynchronizer> logger;
    private readonly string timeSubject;
    private long timeOffset;
    private Task internalTask;
    private Timer? broadcastTimer;
    
    public TimeSynchronizer(
        ApolloConfig config,
        INatsConnection natsConnection,
        ApolloIdGenTimeSource? timeSource = null,
        ILogger<TimeSynchronizer>? logger = null)
    {
        timeSubject = $"{config.DefaultNamespace}.time";
        this.natsConnection = natsConnection;
        this.timeSource = timeSource;
        this.logger = logger ?? NullLogger<TimeSynchronizer>.Instance;
        
        switch (timeSource?.TimeSyncMode)
        {
            case TimeSyncMode.Receive:
                internalTask = SubscribeToTimeMessages();
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
    
    public DateTime CurrentUtc()
    {
        var offset = Interlocked.Read(ref timeOffset);
        return DateTime.UtcNow.AddMilliseconds(offset);
    }

    private async Task SubscribeToTimeMessages()
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

            // get our time source updated first
            timeSource?.SetApolloOffset((long)latency);
            
            // update the local time offset, but... safety first.
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

            await natsConnection.PublishAsync(timeSubject, tickData).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error broadcasting time");
        }
    }
}