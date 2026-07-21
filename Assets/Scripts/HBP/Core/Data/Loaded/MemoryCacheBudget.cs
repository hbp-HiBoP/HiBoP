using System;
using System.Collections.Generic;
using System.Linq;

namespace HBP.Core.Data
{
    public enum MemoryCacheCategory
    {
        ManagedDerived = 0,
        NativeProjection = 1,
        Texture = 2,
        RawRecording = 3
    }

    public readonly struct MemoryCacheSnapshot
    {
        public long LimitBytes { get; }
        public long UsedBytes { get; }
        public long PinnedBytes { get; }
        public bool IsOverBudget => UsedBytes > LimitBytes;

        public MemoryCacheSnapshot(long limitBytes, long usedBytes, long pinnedBytes)
        {
            LimitBytes = limitBytes;
            UsedBytes = usedBytes;
            PinnedBytes = pinnedBytes;
        }
    }

    public sealed class MemoryCacheBudget
    {
        private sealed class Entry
        {
            public object Key;
            public MemoryCacheCategory Category;
            public long Bytes;
            public bool Pinned;
            public long LastAccess;
            public Action Evict;
        }

        private readonly object m_Gate = new();
        private readonly Dictionary<object, Entry> m_Entries = new();
        private long m_Clock;

        public long LimitBytes { get; private set; } = long.MaxValue;
        public event Action<MemoryCacheSnapshot> BudgetExceeded;

        public static long ResolveLimitBytes(int explicitLimitMiB, int totalPhysicalMemoryMiB)
        {
            const long bytesPerMiB = 1024L * 1024L;
            if (explicitLimitMiB > 0)
                return explicitLimitMiB * bytesPerMiB;
            if (totalPhysicalMemoryMiB <= 0)
                return long.MaxValue;

            long ninetyPercentMiB = (long)Math.Floor(totalPhysicalMemoryMiB * 0.9d);
            long reserveTwoGiBMiB = Math.Max(0, totalPhysicalMemoryMiB - 2048L);
            return Math.Min(ninetyPercentMiB, reserveTwoGiBMiB) * bytesPerMiB;
        }

        public void Configure(int explicitLimitMiB, int totalPhysicalMemoryMiB)
        {
            LimitBytes = ResolveLimitBytes(explicitLimitMiB, totalPhysicalMemoryMiB);
            Trim();
        }

        public void Register(object key, MemoryCacheCategory category, long bytes, bool pinned, Action evict)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            if (bytes < 0)
                throw new ArgumentOutOfRangeException(nameof(bytes));

            lock (m_Gate)
            {
                m_Entries[key] = new Entry
                {
                    Key = key,
                    Category = category,
                    Bytes = bytes,
                    Pinned = pinned,
                    LastAccess = ++m_Clock,
                    Evict = evict
                };
            }
            Trim();
        }

        public void Touch(object key)
        {
            lock (m_Gate)
            {
                if (m_Entries.TryGetValue(key, out Entry entry))
                    entry.LastAccess = ++m_Clock;
            }
        }

        public void SetPinned(object key, bool pinned)
        {
            lock (m_Gate)
            {
                if (m_Entries.TryGetValue(key, out Entry entry))
                {
                    entry.Pinned = pinned;
                    entry.LastAccess = ++m_Clock;
                }
            }
            if (!pinned)
                Trim();
        }

        public void Unregister(object key)
        {
            if (key == null)
                return;
            lock (m_Gate)
                m_Entries.Remove(key);
        }

        public MemoryCacheSnapshot GetSnapshot()
        {
            lock (m_Gate)
                return SnapshotLocked();
        }

        public void Clear()
        {
            lock (m_Gate)
                m_Entries.Clear();
        }

        public void Trim()
        {
            List<Action> evictions = new();
            MemoryCacheSnapshot snapshot;
            lock (m_Gate)
            {
                long used = m_Entries.Values.Sum(entry => entry.Bytes);
                foreach (Entry entry in m_Entries.Values
                    .Where(entry => !entry.Pinned)
                    .OrderBy(entry => entry.Category)
                    .ThenBy(entry => entry.LastAccess)
                    .ToArray())
                {
                    if (used <= LimitBytes)
                        break;
                    if (m_Entries.Remove(entry.Key))
                    {
                        used -= entry.Bytes;
                        if (entry.Evict != null)
                            evictions.Add(entry.Evict);
                    }
                }
                snapshot = SnapshotLocked();
            }

            foreach (Action eviction in evictions)
                eviction();
            if (snapshot.IsOverBudget)
                BudgetExceeded?.Invoke(snapshot);
        }

        private MemoryCacheSnapshot SnapshotLocked()
        {
            long used = 0;
            long pinned = 0;
            foreach (Entry entry in m_Entries.Values)
            {
                used += entry.Bytes;
                if (entry.Pinned)
                    pinned += entry.Bytes;
            }
            return new MemoryCacheSnapshot(LimitBytes, used, pinned);
        }
    }
}
