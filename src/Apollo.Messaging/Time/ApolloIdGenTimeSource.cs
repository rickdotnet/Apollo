using Apollo.Configuration;
using IdGen;

namespace Apollo.Messaging.Time;

internal class ApolloIdGenTimeSource : StopwatchTimeSource
{
    public TimeSyncMode TimeSyncMode { get; }
    
    private static readonly DateTimeOffset BaseEpoch = new(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan DefaultTickDuration = TimeSpan.FromMilliseconds(1);
    
    private long apolloOffset = 0;

    public ApolloIdGenTimeSource(TimeSyncMode timeSyncMode = TimeSyncMode.Receive, DateTimeOffset? epoch = null,
        TimeSpan? tickDuration = null)
        : base(epoch ?? BaseEpoch, tickDuration ?? DefaultTickDuration)
    {
        TimeSyncMode = timeSyncMode;
    }
    
    public override long GetTicks() => (Offset.Ticks + Elapsed.Ticks + Interlocked.Read(ref apolloOffset)) / TickDuration.Ticks;
    
    internal void SetApolloOffset(long offset) => Interlocked.Exchange(ref apolloOffset, offset);
}