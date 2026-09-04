using System;
using CRNL.HiBoP.RenderModel;
using UnityEngine;
using UnityEngine.Rendering;

namespace CRNL.HiBoP.XR.StaticRendering
{
    internal static class SurfaceMeshUploader
    {
        internal const float SourceTolerance = 0.000001f;
        internal const float NormalLengthTolerance = 0.001f;

        public static Mesh CreateMesh(SurfaceAsset asset)
        {
            Validate(asset);

            int vertexCount = asset.Positions.Count;
            var positions = new Vector3[vertexCount];
            var normals = new Vector3[vertexCount];
            var uvs = asset.StaticUvs.Count == 0 ? null : new Vector2[vertexCount];
            float scale = asset.CoordinateSpace.MetersPerUnit;

            for (int index = 0; index < vertexCount; index++)
            {
                Float3 position = asset.Positions[index];
                Float3 normal = asset.Normals[index];
                positions[index] = new Vector3(position.X * scale, position.Y * scale, position.Z * scale);
                normals[index] = new Vector3(normal.X, normal.Y, normal.Z);
                if (uvs != null)
                {
                    Float2 uv = asset.StaticUvs[index];
                    uvs[index] = new Vector2(uv.X, uv.Y);
                }
            }

            var indices = new int[asset.Indices.Count];
            for (int index = 0; index < indices.Length; index++)
            {
                indices[index] = checked((int)asset.Indices[index]);
            }

            var mesh = new Mesh
            {
                name = $"P05 Surface {asset.Hash.ToString().Substring(0, 12)}",
                indexFormat = SelectIndexFormat(vertexCount),
            };
            mesh.vertices = positions;
            mesh.normals = normals;
            if (uvs != null)
            {
                mesh.uv = uvs;
            }

            mesh.SetIndices(indices, MeshTopology.Triangles, 0, false);
            mesh.bounds = ConvertBounds(asset.Bounds, scale);
            mesh.UploadMeshData(true);
            return mesh;
        }

        internal static IndexFormat SelectIndexFormat(int vertexCount)
        {
            if (vertexCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(vertexCount));
            }

            return vertexCount <= ushort.MaxValue ? IndexFormat.UInt16 : IndexFormat.UInt32;
        }

        internal static void Validate(SurfaceAsset asset)
        {
            if (asset == null)
            {
                throw new ArgumentNullException(nameof(asset));
            }

            CoordinateSpace space = asset.CoordinateSpace;
            if (space.Handedness != CoordinateHandedness.Left || space.AxisOrder != CoordinateAxisOrder.Xyz || space.Unit != LengthUnit.Millimeter || Mathf.Abs(space.MetersPerUnit - 0.001f) > SourceTolerance || space.MappingVersion != 1 || !space.AssetToBrain.Equals(Matrix4x4F.Identity))
            {
                throw new ArgumentException("P05 accepts only the canonical P03 left-handed XYZ millimetre coordinate space.", nameof(asset));
            }

            Float3 minimum = asset.Positions[0];
            Float3 maximum = minimum;
            for (int index = 0; index < asset.Positions.Count; index++)
            {
                Float3 position = asset.Positions[index];
                minimum = new Float3(Math.Min(minimum.X, position.X), Math.Min(minimum.Y, position.Y), Math.Min(minimum.Z, position.Z));
                maximum = new Float3(Math.Max(maximum.X, position.X), Math.Max(maximum.Y, position.Y), Math.Max(maximum.Z, position.Z));

                Float3 normal = asset.Normals[index];
                float normalLength = Mathf.Sqrt(normal.X * normal.X + normal.Y * normal.Y + normal.Z * normal.Z);
                if (Mathf.Abs(normalLength - 1f) > NormalLengthTolerance)
                {
                    throw new ArgumentException($"Surface normal {index} is not normalized.", nameof(asset));
                }
            }

            if (!Approximately(minimum, asset.Bounds.Minimum) || !Approximately(maximum, asset.Bounds.Maximum))
            {
                throw new ArgumentException("Surface bounds do not match the P03 position buffer.", nameof(asset));
            }
        }

        private static bool Approximately(Float3 left, Float3 right)
        {
            return Math.Abs(left.X - right.X) <= SourceTolerance && Math.Abs(left.Y - right.Y) <= SourceTolerance && Math.Abs(left.Z - right.Z) <= SourceTolerance;
        }

        private static Bounds ConvertBounds(Bounds3F source, float scale)
        {
            Vector3 minimum = new(source.Minimum.X * scale, source.Minimum.Y * scale, source.Minimum.Z * scale);
            Vector3 maximum = new(source.Maximum.X * scale, source.Maximum.Y * scale, source.Maximum.Z * scale);
            var bounds = new Bounds();
            bounds.SetMinMax(minimum, maximum);
            return bounds;
        }
    }
}
