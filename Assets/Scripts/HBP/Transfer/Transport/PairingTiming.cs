using System;
using System.Diagnostics;
using System.Globalization;

namespace HBP.Transfer.Transport
{
    /// <summary>Duration and count diagnostics. Never accepts codes, credentials or user data.</summary>
    public static class PairingTiming
    {
        public static event Action<string> OnMeasured;
        public static IDisposable Measure(string stage) => new Measurement(stage);

        public static void Count(string counter, long value)
        {
            try
            {
                OnMeasured?.Invoke("QUEST-PAIRING counter=" + counter + " value=" + value.ToString(CultureInfo.InvariantCulture));
            }
            catch (Exception)
            {
            }
        }

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
                try
                {
                    OnMeasured?.Invoke(message);
                }
                catch (Exception)
                {
                }
            }
        }
    }
}
