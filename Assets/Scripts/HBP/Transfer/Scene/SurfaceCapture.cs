using System;
using System.Collections.Generic;
using System.Linq;
using HBP.Core.DLL;
using UnityEngine;

namespace HBP.Transfer.Scene
{
    internal sealed class SurfaceCapture
    {
        internal sealed class Geometry
        {
            internal Vector3[] Vertices, Normals;
            internal Vector2[] UV;
            internal Color[] Colors;
            internal int[] Triangles;
            internal long Bytes => 12L * (Vertices.Length + Normals.Length) + 8L * UV.Length + 16L * Colors.Length + 4L * Triangles.Length;
        }

        // Weak source identities do not keep native handles alive. Only this class
        // owns the cached arrays; encoders read them without exposing mutable views.
        private sealed class Entry
        {
            internal WeakReference<Surface> Source;
            internal long GeometryVersion, TransferRevision;
            internal Geometry Data;
        }

        private static readonly LinkedList<Entry> Cache = new();
        private const long Budget = 128L * 1024 * 1024;
        private static long cacheBytes;
        internal readonly Geometry Data;
        internal readonly int[] Mask;
        internal readonly bool Atlas;

        internal SurfaceCapture(Surface source)
        {
            Mask = source.VisibilityMask;
            Atlas = source.IsMarsAtlasLoaded;
            lock (Cache)
            {
                for (var node = Cache.First; node != null;)
                {
                    var next = node.Next;
                    var entry = node.Value;
                    bool alive = entry.Source.TryGetTarget(out var original);
                    bool same = ReferenceEquals(original, source);
                    if (same && entry.GeometryVersion == source.GeometryVersion && entry.TransferRevision == source.TransferRevision)
                    {
                        Data = entry.Data;
                        Cache.Remove(node);
                        Cache.AddFirst(node);
                        return;
                    }

                    if (!alive || same)
                    {
                        cacheBytes -= entry.Data.Bytes;
                        Cache.Remove(node);
                    }

                    node = next;
                }
            }

            using var complete = source.CloneForTransfer();
            complete.UpdateVisibilityMask(Enumerable.Repeat(1, complete.NumberOfTriangles).ToArray()).Dispose();
            var arrays = complete.CopyTransferBuffers();
            Data = new Geometry
            {
                Vertices = arrays.vertices,
                Normals = arrays.normals,
                Triangles = arrays.triangles,
                UV = arrays.uv,
                Colors = arrays.colors
            };
            if (Data.Bytes > Budget)
            {
                return;
            }

            lock (Cache)
            {
                while (cacheBytes + Data.Bytes > Budget && Cache.Last != null)
                {
                    cacheBytes -= Cache.Last.Value.Data.Bytes;
                    Cache.RemoveLast();
                }

                Cache.AddFirst(new Entry { Source = new WeakReference<Surface>(source), GeometryVersion = source.GeometryVersion, TransferRevision = source.TransferRevision, Data = Data });
                cacheBytes += Data.Bytes;
            }
        }
    }
}
