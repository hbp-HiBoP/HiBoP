using System;
using System.Collections.Generic;

namespace HBP.Core.Tools
{
    /// <summary>Protects shared native resources against release while a consumer or reservation owns them.</summary>
    public static class ResourceRetention
    {
        private static readonly object s_Gate = new();
        private static readonly Dictionary<string, int> s_Users = new(StringComparer.Ordinal);
        private static readonly Dictionary<string, Reservation> s_Reservations = new(StringComparer.Ordinal);
        public static event Func<string, string> ReleaseBlockReason;
        [ThreadStatic] private static Reservation s_Releasing;

        public static IDisposable Retain(string resource)
        {
            lock (s_Gate)
            {
                EnsureCanUse(resource);
                s_Users.TryGetValue(resource, out int users);
                s_Users[resource] = users + 1;
                return new Usage(resource);
            }
        }

        public static void EnsureCanUse(string resource)
        {
            lock (s_Gate)
                if (s_Reservations.ContainsKey(resource))
                    throw new InvalidOperationException("The resource is being unloaded: " + resource);
        }

        public static string GetReleaseBlockReason(string resource)
        {
            lock (s_Gate)
            {
                if (s_Users.TryGetValue(resource, out int users) && users > 0) return "A task is using " + resource + ".";
                if (s_Reservations.TryGetValue(resource, out var reservation) && reservation != s_Releasing) return "An unload is already pending for " + resource + ".";
                if (ReleaseBlockReason != null)
                    foreach (Func<string, string> probe in ReleaseBlockReason.GetInvocationList())
                    {
                        string reason = probe(resource);
                        if (!string.IsNullOrEmpty(reason)) return reason;
                    }

                return null;
            }
        }

        public static void EnsureCanRelease(string resource)
        {
            string reason = GetReleaseBlockReason(resource);
            if (reason != null) throw new InvalidOperationException(reason);
        }

        public static Reservation ReserveRelease(string resource)
        {
            lock (s_Gate)
            {
                EnsureCanRelease(resource);
                var reservation = new Reservation(resource);
                s_Reservations.Add(resource, reservation);
                return reservation;
            }
        }

        public sealed class Reservation : IDisposable
        {
            public string Resource { get; }
            private bool m_Disposed;
            internal Reservation(string resource) => Resource = resource;

            public void Release(Action release)
            {
                lock (s_Gate)
                {
                    if (m_Disposed) throw new ObjectDisposedException(nameof(Reservation));
                    var previous = s_Releasing;
                    s_Releasing = this;
                    try
                    {
                        EnsureCanRelease(Resource);
                        release();
                    }
                    finally
                    {
                        s_Releasing = previous;
                        Dispose();
                    }
                }
            }

            public void Dispose()
            {
                lock (s_Gate)
                {
                    if (m_Disposed) return;
                    m_Disposed = true;
                    s_Reservations.Remove(Resource);
                }
            }
        }

        private sealed class Usage : IDisposable
        {
            private string m_Resource;
            public Usage(string resource) => m_Resource = resource;

            public void Dispose()
            {
                lock (s_Gate)
                {
                    if (m_Resource == null) return;
                    if (--s_Users[m_Resource] == 0) s_Users.Remove(m_Resource);
                    m_Resource = null;
                }
            }
        }
    }
}
