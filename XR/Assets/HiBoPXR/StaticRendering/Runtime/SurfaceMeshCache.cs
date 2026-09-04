using System;
using System.Collections.Generic;
using CRNL.HiBoP.Contracts;
using CRNL.HiBoP.RenderModel;
using UnityEngine;

namespace CRNL.HiBoP.XR.StaticRendering
{
    internal static class SurfaceMeshCache
    {
        private static readonly Dictionary<AssetHash, Entry> Entries = new();

        internal static int ActiveMeshCount => Entries.Count;

        public static SurfaceMeshLease Acquire(SurfaceAsset asset)
        {
            if (asset == null)
            {
                throw new ArgumentNullException(nameof(asset));
            }

            if (Entries.TryGetValue(asset.Hash, out Entry entry))
            {
                entry.ReferenceCount++;
                return new SurfaceMeshLease(asset.Hash, entry.Mesh);
            }

            Mesh mesh = SurfaceMeshUploader.CreateMesh(asset);
            Entries.Add(asset.Hash, new Entry(mesh));
            return new SurfaceMeshLease(asset.Hash, mesh);
        }

        internal static void Release(AssetHash hash)
        {
            if (!Entries.TryGetValue(hash, out Entry entry))
            {
                return;
            }

            entry.ReferenceCount--;
            if (entry.ReferenceCount > 0)
            {
                return;
            }

            Entries.Remove(hash);
            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(entry.Mesh);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(entry.Mesh);
            }
        }

        internal static void ClearForTests()
        {
            foreach (Entry entry in Entries.Values)
            {
                UnityEngine.Object.DestroyImmediate(entry.Mesh);
            }

            Entries.Clear();
        }

        private sealed class Entry
        {
            public Entry(Mesh mesh)
            {
                Mesh = mesh;
                ReferenceCount = 1;
            }

            public Mesh Mesh { get; }
            public int ReferenceCount { get; set; }
        }
    }

    internal sealed class SurfaceMeshLease : IDisposable
    {
        private AssetHash m_Hash;
        private bool m_Disposed;

        internal SurfaceMeshLease(AssetHash hash, Mesh mesh)
        {
            m_Hash = hash;
            Mesh = mesh;
        }

        public Mesh Mesh { get; }

        public void Dispose()
        {
            if (m_Disposed)
            {
                return;
            }

            m_Disposed = true;
            SurfaceMeshCache.Release(m_Hash);
            m_Hash = default;
        }
    }
}
