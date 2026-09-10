using System;
using HBP.Transfer.Anatomy;
using UnityEngine;
using UnityEngine.Rendering;

namespace HBP.Quest.Legacy
{
    /// <summary>Adapted from SurfaceMeshUploader at eb26c323e. No scientific calculation.</summary>
    public static class AnatomyMeshUploader
    {
        public const string FrameId = "hibop-mni-unity-mm-v1";

        public static Mesh CreateMesh(AnatomySnapshot snapshot)
        {
            Validate(snapshot);
            var positions = new Vector3[snapshot.VertexCount];
            var normals = new Vector3[snapshot.VertexCount];
            var uvs = snapshot.Uvs.Count == 0 ? null : new Vector2[snapshot.VertexCount];
            for (int i = 0; i < positions.Length; i++)
            {
                int j = i * 3;
                // Keep prepared millimetres bit-exact. The prefab owns mm -> m.
                positions[i] = new Vector3(snapshot.Positions[j], snapshot.Positions[j + 1], snapshot.Positions[j + 2]);
                normals[i] = new Vector3(snapshot.Normals[j], snapshot.Normals[j + 1], snapshot.Normals[j + 2]);
                if (uvs != null) uvs[i] = new Vector2(snapshot.Uvs[i * 2], snapshot.Uvs[i * 2 + 1]);
            }

            var indices = new int[snapshot.Indices.Count];
            for (int i = 0; i < indices.Length; i++) indices[i] = checked((int)snapshot.Indices[i]);
            if (snapshot.Winding == AnatomyWinding.CounterClockwise)
                for (int i = 0; i < indices.Length; i += 3)
                    (indices[i + 1], indices[i + 2]) = (indices[i + 2], indices[i + 1]);

            var mesh = new Mesh { name = "Quest Anatomy", indexFormat = positions.Length > ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            try
            {
                mesh.vertices = positions;
                mesh.normals = normals;
                if (uvs != null) mesh.uv = uvs;
                mesh.SetIndices(indices, MeshTopology.Triangles, 0, true);
                return mesh;
            }
            catch
            {
                Release(mesh);
                throw;
            }
        }

        private static void Validate(AnatomySnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            var space = snapshot.Coordinates;
            if (space.FrameId != FrameId || space.Handedness != AnatomyHandedness.Left || space.Unit != AnatomyLengthUnit.Millimeter || space.MappingVersion != 1)
                throw new ArgumentException("Quest anatomy requires the prepared HiBoP MNI Unity millimetre frame, version 1.");
            for (int i = 0; i < 16; i++)
                if (space.AssetToBrain[i] != (i % 5 == 0 ? 1f : 0f))
                    throw new ArgumentException("Quest anatomy requires an identity AssetToBrain mapping; bake other mappings upstream.");
            if (snapshot.Color[3] != 1f) throw new ArgumentException("Quest anatomy currently supports only opaque snapshots.");
        }

        internal static void Release(Mesh mesh)
        {
            if (mesh == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(mesh);
            else UnityEngine.Object.DestroyImmediate(mesh);
        }
    }
}
