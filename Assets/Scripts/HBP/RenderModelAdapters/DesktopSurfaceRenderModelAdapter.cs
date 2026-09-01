using System;
using CRNL.HiBoP.Contracts;
using CRNL.HiBoP.RenderModel;
using HBP.Core.Data;
using HBP.Core.DLL;
using HBP.Data.Module3D;
using UnityEngine;

namespace HBP.RenderModelAdapters
{
    public static class DesktopSurfaceRenderModelAdapter
    {
        public static SurfaceAsset CaptureAsset(Surface source, AssetHash hash, SurfaceRepresentation representation)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            Mesh mesh = new();
            try
            {
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                source.UpdateMeshFromDLL(mesh);
                return CaptureAsset(mesh, hash, representation);
            }
            finally
            {
                if (Application.isPlaying)
                    UnityEngine.Object.Destroy(mesh);
                else
                    UnityEngine.Object.DestroyImmediate(mesh);
            }
        }

        public static SurfaceAsset CaptureAsset(Mesh source, AssetHash hash, SurfaceRepresentation representation)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            Vector3[] sourcePositions = source.vertices;
            Vector3[] sourceNormals = source.normals;
            Vector2[] sourceUvs = source.uv;
            int[] sourceIndices = source.triangles;
            Float3[] positions = Convert(sourcePositions);
            Float3[] normals = Convert(sourceNormals);
            Float2[] uvs = Convert(sourceUvs);
            uint[] indices = new uint[sourceIndices.Length];
            for (int index = 0; index < sourceIndices.Length; index++)
                indices[index] = checked((uint)sourceIndices[index]);

            return new SurfaceAsset(hash, representation, CoordinateSpace.DesktopUnityMillimetersV1, Convert(source.bounds), RenderBuffer<Float3>.TakeOwnership(positions), RenderBuffer<Float3>.TakeOwnership(normals), RenderBuffer<uint>.TakeOwnership(indices), RenderBuffer<Float2>.TakeOwnership(uvs));
        }

        public static SurfaceFrame CaptureFrame(Column3DDynamic source, AssetHash surfaceAssetHash, StateRevision sourceStateRevision)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (source.SurfaceGenerator == null)
                throw new InvalidOperationException("The column has no surface generator.");
            TemporalSample sample = source.CurrentProjectionSample;
            return CaptureFrame(source.SurfaceGenerator, surfaceAssetHash, sourceStateRevision, new RenderTemporalSample(sample.Index, sample.Alpha));
        }

        public static SurfaceFrame CaptureFrame(SurfaceGenerator source, AssetHash surfaceAssetHash, StateRevision sourceStateRevision, RenderTemporalSample sample)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            return CaptureFrame(source.ActivityUV, source.AlphaUV, surfaceAssetHash, sourceStateRevision, sample);
        }

        public static SurfaceFrame CaptureFrame(Vector2[] activityUvs, Vector2[] opacityUvs, AssetHash surfaceAssetHash, StateRevision sourceStateRevision, RenderTemporalSample sample)
        {
            if (activityUvs == null || opacityUvs == null || activityUvs.Length != opacityUvs.Length)
                throw new InvalidOperationException("Surface generator UV buffers are unavailable or inconsistent.");

            float[] activity = new float[activityUvs.Length];
            float[] opacity = new float[activityUvs.Length];
            byte[] active = new byte[activityUvs.Length];
            for (int index = 0; index < activityUvs.Length; index++)
            {
                float activitySentinel = activityUvs[index].y;
                float opacitySentinel = opacityUvs[index].y;
                if (activitySentinel != opacitySentinel || (activitySentinel != 0f && activitySentinel != 1f))
                    throw new ArgumentException($"Surface UV sentinels must match and be exactly 0 or 1 at vertex {index}.");
                activity[index] = activityUvs[index].x;
                opacity[index] = opacityUvs[index].x;
                active[index] = activitySentinel == 0f ? (byte)1 : (byte)0;
            }

            // Desktop surfaces use the lower native sample. TemporalAlpha remains provenance only.
            return new SurfaceFrame(surfaceAssetHash, sourceStateRevision, sample, TemporalApplication.SampleAndHold, RenderBuffer<float>.TakeOwnership(activity), RenderBuffer<float>.TakeOwnership(opacity), RenderBuffer<byte>.TakeOwnership(active));
        }

        internal static Bounds3F Convert(Bounds bounds)
        {
            return new Bounds3F(Convert(bounds.min), Convert(bounds.max));
        }

        internal static Float3 Convert(Vector3 value) => new(value.x, value.y, value.z);
        internal static Float2 Convert(Vector2 value) => new(value.x, value.y);
        internal static Rgba32 Convert(Color32 value) => new(value.r, value.g, value.b, value.a);

        internal static Float3[] Convert(Vector3[] values)
        {
            Float3[] result = new Float3[values.Length];
            for (int index = 0; index < values.Length; index++)
                result[index] = Convert(values[index]);
            return result;
        }

        internal static Float2[] Convert(Vector2[] values)
        {
            Float2[] result = new Float2[values.Length];
            for (int index = 0; index < values.Length; index++)
                result[index] = Convert(values[index]);
            return result;
        }
    }
}
