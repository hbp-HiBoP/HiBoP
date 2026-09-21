using System;
using System.Threading;

namespace HBP.Sync.Testing
{
    public sealed class FakeMonotonicClock : IMonotonicClock
    {
        private long m_Timestamp;

        public FakeMonotonicClock(long frequency = TimeSpan.TicksPerSecond)
        {
            if (frequency <= 0)
                throw new ArgumentOutOfRangeException(nameof(frequency));
            Frequency = frequency;
        }

        public long Frequency { get; }
        public long GetTimestamp() => Interlocked.Read(ref m_Timestamp);

        public void Advance(TimeSpan duration)
        {
            if (duration < TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(duration));
            long ticks = checked((long)(duration.TotalSeconds * Frequency));
            Interlocked.Add(ref m_Timestamp, ticks);
        }
    }
}
