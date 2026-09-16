using System;
using System.Collections.Generic;
using System.IO;

namespace HBP.Core.Tools
{
    // Only the archive reader may register its freshly written, hash-verified files.
    // This is an ownership contract for a private workspace, not a cache of arbitrary paths.
    internal sealed class VerifiedResourceScope : IDisposable
    {
        private readonly object gate = new();
        private readonly Dictionary<string, Entry> entries = new(StringComparer.Ordinal);
        private readonly Action cleanup;
        private bool sealedContent, disposed, cleaned;
        private int readers;

        internal VerifiedResourceScope(Action cleanup)
        {
            this.cleanup = cleanup;
        }

        internal void Register(string path, string hash)
        {
            lock (gate)
            {
                if (disposed || sealedContent) throw new InvalidOperationException("Resource workspace is immutable.");
                var guard = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                try
                {
                    entries.Add(path, new Entry(guard, hash));
                }
                catch
                {
                    guard.Dispose();
                    throw;
                }
            }
        }

        internal void Seal()
        {
            lock (gate)
            {
                if (disposed) throw new ObjectDisposedException(nameof(VerifiedResourceScope));
                sealedContent = true;
            }
        }

        internal Lease Acquire(string path, string companion = null)
        {
            lock (gate)
            {
                if (disposed) throw new ObjectDisposedException(nameof(VerifiedResourceScope));
                if (!sealedContent) throw new InvalidOperationException("Archive validation is incomplete.");
                if (!entries.TryGetValue(path, out var entry) || (companion != null && !entries.ContainsKey(companion)))
                    throw new InvalidDataException("Resource was not verified by this workspace.");
                readers++;
                return new Lease(this, path, entry.Hash, companion == null ? null : entries[companion].Hash);
            }
        }

        public void Dispose()
        {
            lock (gate)
            {
                disposed = true;
                TryCleanup();
            }
        }

        private void Release()
        {
            lock (gate)
            {
                readers--;
                TryCleanup();
            }
        }

        private void TryCleanup()
        {
            if (!disposed || readers != 0 || cleaned) return;
            cleaned = true;
            foreach (var entry in entries.Values) entry.Guard.Dispose();
            entries.Clear();
            cleanup();
        }

        private sealed class Entry
        {
            internal readonly FileStream Guard;
            internal readonly string Hash;

            internal Entry(FileStream guard, string hash)
            {
                Guard = guard;
                Hash = hash;
            }
        }

        internal sealed class Lease : IDisposable
        {
            private VerifiedResourceScope owner;
            internal string Path { get; }
            internal string Hash { get; }
            internal string CompanionHash { get; }

            internal Lease(VerifiedResourceScope owner, string path, string hash, string companionHash)
            {
                this.owner = owner;
                Path = path;
                Hash = hash;
                CompanionHash = companionHash;
            }

            internal void RequireAlive()
            {
                if (owner == null) throw new ObjectDisposedException(nameof(Lease));
            }

            public void Dispose()
            {
                System.Threading.Interlocked.Exchange(ref owner, null)?.Release();
            }
        }
    }
}
