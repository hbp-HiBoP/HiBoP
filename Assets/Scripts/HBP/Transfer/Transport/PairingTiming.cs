using System;
using System.Diagnostics;
using System.Globalization;

namespace HBP.Transfer.Transport
{
    /// <summary>Duration-only diagnostics. Never accepts codes, credentials or user data.</summary>
    public static class PairingTiming
    {
        public static event Action<string> Measured;
        public static IDisposable Measure(string stage) => new Measurement(stage);

        private sealed class Measurement : IDisposable
        {
            private readonly string stage;
            private readonly Stopwatch clock = Stopwatch.StartNew();
            private bool disposed;
            public Measurement(string stage) => this.stage = stage;
            public void Dispose()
            {
                if (disposed) return;
                disposed = true;
                string message = "QUEST-PAIRING stage=" + stage + " elapsedMs=" + clock.Elapsed.TotalMilliseconds.ToString("F2", CultureInfo.InvariantCulture);
                try { Measured?.Invoke(message); } catch (Exception) { }
            }
        }
    }
}
