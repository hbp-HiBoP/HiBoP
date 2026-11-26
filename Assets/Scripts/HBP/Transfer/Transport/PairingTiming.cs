using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;

namespace HBP.Transfer.Transport
{
    /// <summary>Duration and count diagnostics. Never accepts codes, credentials or user data.</summary>
    public static class PairingTiming
    {
        public static event Action<string> OnMeasured;
        public static bool Enabled => OnMeasured != null;
        public static IDisposable Measure(string stage, string scope = null, long attempt = 0) => OnMeasured == null ? null : new Measurement(stage, scope, attempt);

        // Each accumulator belongs to one producer, consumer or serial async loop.
        // Emit a single summary outside its hot path, including samples from interrupted work.
        public static Accumulator Aggregate(string stage, string scope, long attempt = 0) => scope == null || OnMeasured == null ? null : new Accumulator(stage, scope, attempt);

        public static Sample Measure(Accumulator accumulator, long bytes = 0) => new(accumulator, bytes);

        public static void Link(string kind, string id, string scope)
        {
            if (id != null && scope != null && OnMeasured != null)
                Emit("QUEST-PAIRING link=" + kind + " id=" + id + " scope=" + scope);
        }

        public static void Count(string counter, long value, string scope = null, long attempt = 0)
        {
            if (OnMeasured != null)
                Emit("QUEST-PAIRING counter=" + counter + " value=" + value.ToString(CultureInfo.InvariantCulture) + Scope(scope, attempt));
        }

        private static string Scope(string scope, long attempt) => (scope == null ? "" : " scope=" + scope) + (attempt == 0 ? "" : " attempt=" + attempt.ToString(CultureInfo.InvariantCulture));

        private static string Milliseconds(long ticks) => (ticks * 1000.0 / Stopwatch.Frequency).ToString("F2", CultureInfo.InvariantCulture);

        private static void Emit(string message)
        {
            try
            {
                OnMeasured?.Invoke(message);
            }
            catch (Exception)
            {
            }
        }

        public sealed class Accumulator : IDisposable
        {
            private readonly string stage;
            private readonly string scope;
            private readonly long attempt;
            private long first, last, elapsed, samples, bytes;

            internal Accumulator(string stage, string scope, long attempt)
            {
                this.stage = stage;
                this.scope = scope;
                this.attempt = attempt;
            }

            internal void Add(long start, long end, long size)
            {
                if (samples == 0) first = start;
                last = end;
                elapsed += end - start;
                samples++;
                bytes += size;
            }

            public void Dispose()
            {
                if (samples == 0) return;
                Emit("QUEST-PAIRING stage=" + stage + " elapsedMs=" + Milliseconds(elapsed) + Scope(scope, attempt) + " startMs=" + Milliseconds(first) + " endMs=" + Milliseconds(last) + " samples=" + samples.ToString(CultureInfo.InvariantCulture) + " bytes=" + bytes.ToString(CultureInfo.InvariantCulture));
                elapsed = samples = bytes = 0;
            }
        }

        public readonly struct Sample : IDisposable
        {
            private readonly Accumulator accumulator;
            private readonly long bytes;
            private readonly long start;

            internal Sample(Accumulator accumulator, long bytes)
            {
                this.accumulator = accumulator;
                this.bytes = bytes;
                start = accumulator == null ? 0 : Stopwatch.GetTimestamp();
            }

            public void Dispose() => accumulator?.Add(start, Stopwatch.GetTimestamp(), bytes);
        }

        private sealed class Measurement : IDisposable
        {
            private readonly string stage;
            private readonly string scope;
            private readonly long attempt;
            private readonly int startThread = Thread.CurrentThread.ManagedThreadId;
            private readonly long start = Stopwatch.GetTimestamp();
            private bool disposed;

            public Measurement(string stage, string scope, long attempt)
            {
                this.stage = stage;
                this.scope = scope;
                this.attempt = attempt;
            }

            public void Dispose()
            {
                if (disposed) return;
                disposed = true;
                // Async spans measure wall time, including waits, rather than worker CPU time.
                long end = Stopwatch.GetTimestamp();
                Emit("QUEST-PAIRING stage=" + stage + " elapsedMs=" + Milliseconds(end - start) + Scope(scope, attempt) + " startMs=" + Milliseconds(start) + " endMs=" + Milliseconds(end) + " startThread=" + startThread.ToString(CultureInfo.InvariantCulture) + " endThread=" + Thread.CurrentThread.ManagedThreadId.ToString(CultureInfo.InvariantCulture));
            }
        }
    }
}
