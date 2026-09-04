using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using CRNL.HiBoP.Contracts;
using CRNL.HiBoP.Protocol;
using CRNL.HiBoP.RenderModel;

namespace CRNL.HiBoP.XR.RemoteAssets
{
    public sealed class RemoteSurfaceAssetStore
    {
        private readonly object m_Gate = new();
        private readonly InMemoryRemoteAssetCache m_Cache;
        private readonly Dictionary<AssetHash, Entry> m_Entries = new();

        public RemoteSurfaceAssetStore(InMemoryRemoteAssetCache cache)
        {
            m_Cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        public bool TryAcquire(AssetHash hash, out RemoteSurfaceAssetLease lease)
        {
            if (!m_Cache.TryAcquire(hash, out RemoteAssetLease payloadLease))
            {
                lease = null;
                return false;
            }

            try
            {
                lock (m_Gate)
                {
                    if (!m_Entries.TryGetValue(hash, out Entry entry))
                    {
                        if (payloadLease.Descriptor.Kind != RemoteAssetKind.Surface)
                            throw new InvalidDataException("The surface store accepts only surface assets.");
                        using Stream payload = payloadLease.OpenRead();
                        SurfaceAsset surface = SurfaceAssetPayloadCodec.Decode(payload, hash, payloadLease.Descriptor.EncodedBytes);
                        if (surface.Positions.Count != payloadLease.Descriptor.PrimaryCount || surface.Indices.Count != payloadLease.Descriptor.SecondaryCount || surface.StaticUvs.Count != payloadLease.Descriptor.TertiaryCount)
                            throw new InvalidDataException("The decoded surface dimensions do not match the validated descriptor.");
                        entry = new Entry(surface);
                        m_Entries.Add(hash, entry);
                    }

                    entry.ReferenceCount++;
                    lease = new RemoteSurfaceAssetLease(this, payloadLease, entry.Asset);
                    return true;
                }
            }
            catch
            {
                payloadLease.Dispose();
                throw;
            }
        }

        internal void Release(RemoteAssetLease payloadLease)
        {
            lock (m_Gate)
            {
                AssetHash hash = payloadLease.Hash;
                if (!m_Entries.TryGetValue(hash, out Entry entry))
                    throw new InvalidOperationException("The decoded surface lease is not registered.");
                entry.ReferenceCount--;
                if (entry.ReferenceCount == 0)
                    m_Entries.Remove(hash);
            }

            payloadLease.Dispose();
        }

        private sealed class Entry
        {
            public Entry(SurfaceAsset asset)
            {
                Asset = asset;
            }

            public SurfaceAsset Asset { get; }

            public int ReferenceCount { get; set; }
        }
    }

    public sealed class RemoteSurfaceAssetLease : IDisposable
    {
        private RemoteSurfaceAssetStore m_Owner;
        private RemoteAssetLease m_PayloadLease;

        internal RemoteSurfaceAssetLease(RemoteSurfaceAssetStore owner, RemoteAssetLease payloadLease, SurfaceAsset asset)
        {
            m_Owner = owner;
            m_PayloadLease = payloadLease;
            Asset = asset;
        }

        public SurfaceAsset Asset { get; }

        public AssetHash Hash => Asset.Hash;

        public void Dispose()
        {
            RemoteSurfaceAssetStore owner = Interlocked.Exchange(ref m_Owner, null);
            if (owner == null)
                return;
            RemoteAssetLease payloadLease = Interlocked.Exchange(ref m_PayloadLease, null);
            owner.Release(payloadLease);
        }
    }
}
