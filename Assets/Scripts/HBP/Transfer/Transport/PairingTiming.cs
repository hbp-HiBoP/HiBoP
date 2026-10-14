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
        public static IDisposable Measure(string stage, string scope = null) => new Measurement(stage, scope);

        public static void Count(string counter, long value, string scope = null)
        {
            try
            {
                OnMeasured?.Invoke("QUEST-PAIRING counter=" + counter + " value=" + value.ToString(CultureInfo.InvariantCulture) + (scope == null ? "" : " scope=" + scope));
            }
            catch (Exception)
            {
            }
        }

        private sealed class Measurement : IDisposable
        {
            private readonly string stage;
            private readonly string scope;
            private readonly int startThread = Thread.CurrentThread.ManagedThreadId;
            private readonly Stopwatch clock = Stopwatch.StartNew();
            private bool disposed;

            public Measurement(string stage, string scope)
            {
                this.stage = stage;
                this.scope = scope;
            }

            public void Dispose()
            {
                if (disposed) return;
                disposed = true;
                // Async spans measure wall time, including waits, rather than worker CPU time.
                string message = "QUEST-PAIRING stage=" + stage + " elapsedMs=" + clock.Elapsed.TotalMilliseconds.ToString("F2", CultureInfo.InvariantCulture) + (scope == null ? "" : " scope=" + scope) + " startThread=" + startThread.ToString(CultureInfo.InvariantCulture) + " endThread=" + Thread.CurrentThread.ManagedThreadId.ToString(CultureInfo.InvariantCulture);
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
