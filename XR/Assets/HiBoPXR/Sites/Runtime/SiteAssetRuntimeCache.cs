using System;
using System.Collections.Generic;
using CRNL.HiBoP.Contracts;
using CRNL.HiBoP.RenderModel;
using UnityEngine;

namespace CRNL.HiBoP.XR.Sites
{
    internal static class SiteAssetRuntimeCache
    {
        private static readonly Dictionary<AssetHash, Entry> s_Entries = new();

        internal static int ActiveAssetCount => s_Entries.Count;

        internal static SiteAssetLease Acquire(SiteAsset asset)
        {
            if (asset == null)
                throw new ArgumentNullException(nameof(asset));
            ValidateCoordinateSpace(asset.CoordinateSpace);
            if (!s_Entries.TryGetValue(asset.Hash, out Entry entry))
            {
                entry = new Entry(asset);
                s_Entries.Add(asset.Hash, entry);
            }
            else if (!entry.Matches(asset))
            {
                throw new InvalidOperationException("A site hash cannot identify different site IDs or positions.");
            }

            entry.ReferenceCount++;
            return new SiteAssetLease(asset.Hash, entry);
        }

        internal static void ClearForTests()
        {
            foreach (Entry entry in s_Entries.Values)
                entry.Dispose();
            s_Entries.Clear();
        }

        private static void Release(AssetHash hash, Entry entry)
        {
            if (!s_Entries.TryGetValue(hash, out Entry current) || !ReferenceEquals(entry, current))
                return;
            entry.ReferenceCount--;
            if (entry.ReferenceCount > 0)
                return;
            s_Entries.Remove(hash);
            entry.Dispose();
        }

        private static void ValidateCoordinateSpace(CoordinateSpace space)
        {
            CoordinateSpace canonical = CoordinateSpace.DesktopUnityMillimetersV1;
            if (space.Handedness != canonical.Handedness || space.AxisOrder != canonical.AxisOrder || space.Unit != canonical.Unit || space.MetersPerUnit != canonical.MetersPerUnit || space.MappingVersion != canonical.MappingVersion || !space.AssetToBrain.Equals(canonical.AssetToBrain))
                throw new ArgumentException("P10 accepts only the canonical P03 millimeter coordinate space.", nameof(space));
        }

        internal sealed class Entry : IDisposable
        {
            public Entry(SiteAsset asset)
            {
                Asset = asset;
                Index = new SiteBvh(asset);
                var positions = new Vector4[asset.Positions.Count];
                for (int index = 0; index < positions.Length; index++)
                {
                    Float3 value = asset.Positions[index];
                    positions[index] = new Vector4(value.X, value.Y, value.Z, 0f);
                }

                PositionBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, positions.Length, sizeof(float) * 4);
                PositionBuffer.SetData(positions);
            }

            public SiteAsset Asset { get; }

            public SiteBvh Index { get; }

            public GraphicsBuffer PositionBuffer { get; }

            public int ReferenceCount { get; set; }

            public bool Matches(SiteAsset candidate)
            {
                if (Asset.SiteIds.Count != candidate.SiteIds.Count || !Asset.Bounds.Equals(candidate.Bounds) || !Asset.CoordinateSpace.Equals(candidate.CoordinateSpace))
                    return false;
                for (int index = 0; index < Asset.SiteIds.Count; index++)
                {
                    if (Asset.SiteIds[index] != candidate.SiteIds[index] || !Asset.Positions[index].Equals(candidate.Positions[index]))
                        return false;
                }

                return true;
            }

            public void Dispose() => PositionBuffer.Dispose();
        }

        internal sealed class SiteAssetLease : IDisposable
        {
            private readonly AssetHash m_Hash;
            private Entry m_Entry;

            internal SiteAssetLease(AssetHash hash, Entry entry)
            {
                m_Hash = hash;
                m_Entry = entry;
            }

            public SiteAsset Asset => m_Entry?.Asset;

            public SiteBvh Index => m_Entry?.Index;

            public GraphicsBuffer PositionBuffer => m_Entry?.PositionBuffer;

            public void Dispose()
            {
                if (m_Entry == null)
                    return;
                Entry entry = m_Entry;
                m_Entry = null;
                Release(m_Hash, entry);
            }
        }
    }
}
