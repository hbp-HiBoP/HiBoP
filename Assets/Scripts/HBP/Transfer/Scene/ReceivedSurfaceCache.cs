using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace HBP.Transfer.Scene
{
    /// <summary>Bounded immutable surface bytes; each consumer still creates its own native object.</summary>
    public sealed class ReceivedSurfaceCache
    {
        private readonly object gate = new();
        private readonly Dictionary<string, LinkedListNode<(string key, byte[] bytes)>> entries = new(StringComparer.Ordinal);
        private readonly LinkedList<(string key, byte[] bytes)> lru = new();
        private readonly long budget;
        private long bytes, generation;

        internal long Generation
        {
            get
            {
                lock (gate)
                    return generation;
            }
        }

        public ReceivedSurfaceCache(long budgetBytes = 128L * 1024 * 1024)
        {
            if (budgetBytes < 0)
                throw new ArgumentOutOfRangeException(nameof(budgetBytes));
            budget = budgetBytes;
        }

        public void Clear()
        {
            lock (gate)
            {
                generation++;
                entries.Clear();
                lru.Clear();
                bytes = 0;
            }
        }

        // Caller has already verified the ENTIRE incoming archive and this buffer's SHA.
        // An open range is supplied even on hits, so a missing/disposed source cannot be hidden.
        internal Stream Open(string name, Stream verified, CancellationToken token, long expectedGeneration)
        {
            string key = "surface-v1:" + name;
            long startedGeneration;
            lock (gate)
            {
                startedGeneration = generation;
                if (generation != expectedGeneration)
                    return verified;
                if (entries.TryGetValue(key, out var entry))
                {
                    lru.Remove(entry);
                    lru.AddFirst(entry);
                    verified.Dispose();
                    return new MemoryStream(entry.Value.bytes, false);
                }
            }

            if (verified.Length > budget || verified.Length > int.MaxValue)
            {
                return verified;
            }

            byte[] content;
            using (verified)
            {
                content = new byte[(int)verified.Length];
                for (int offset = 0; offset < content.Length;)
                {
                    token.ThrowIfCancellationRequested();
                    int read = verified.Read(content, offset, Math.Min(65536, content.Length - offset));
                    if (read == 0)
                        throw new EndOfStreamException();
                    offset += read;
                }
            }

            lock (gate)
            {
                if (generation == startedGeneration && !entries.ContainsKey(key))
                {
                    while (bytes + content.Length > budget && lru.Last != null)
                    {
                        bytes -= lru.Last.Value.bytes.Length;
                        entries.Remove(lru.Last.Value.key);
                        lru.RemoveLast();
                    }

                    entries.Add(key, lru.AddFirst((key, content)));
                    bytes += content.Length;
                }
            }

            return new MemoryStream(content, false);
        }
    }
}
