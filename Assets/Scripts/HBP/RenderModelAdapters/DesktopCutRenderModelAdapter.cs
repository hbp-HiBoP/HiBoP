using System;
using System.Collections.Generic;
using CRNL.HiBoP.Contracts;
using CRNL.HiBoP.RenderModel;
using HBP.Core.DLL;
using UnityEngine;

namespace HBP.RenderModelAdapters
{
    public static class DesktopCutRenderModelAdapter
    {
        public static TextureAsset CaptureBaseTexture(CutGenerator source, AssetHash hash, TextureColorSpace colorSpace)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            Vector2Int size = source.CutGeometryGenerator.TextureSize;
            return new TextureAsset(hash, size.x, size.y, colorSpace, RenderBuffer<Rgba32>.TakeOwnership(Convert(source.CopyBasePixels())));
        }

        public static CutOverlayFrame CaptureOverlay(CutGenerator source, ContractId cutId, ContractId columnId, StateRevision sourceStateRevision, RenderTemporalSample sample, ScopeRevision mappingRevision)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            Vector2Int size = source.CutGeometryGenerator.TextureSize;
            return CaptureOverlay(source.CopyOverlayPixels(), size.x, size.y, cutId, columnId, sourceStateRevision, sample, mappingRevision);
        }

        public static CutOverlayFrame CaptureOverlay(Color32[] pixels, int width, int height, ContractId cutId, ContractId columnId, StateRevision sourceStateRevision, RenderTemporalSample sample, ScopeRevision mappingRevision)
        {
            if (pixels == null)
                throw new ArgumentNullException(nameof(pixels));
            // Desktop cuts use the lower native sample. TemporalAlpha remains provenance only.
            return new CutOverlayFrame(cutId, columnId, sourceStateRevision, width, height, sample, TemporalApplication.SampleAndHold, mappingRevision, RenderBuffer<Rgba32>.TakeOwnership(Convert(pixels)));
        }

        public static CutGeometryAsset CaptureGeometry(Mesh source, AssetHash hash)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            int[] sourceIndices = source.triangles;
            uint[] indices = new uint[sourceIndices.Length];
            for (int index = 0; index < sourceIndices.Length; index++)
                indices[index] = checked((uint)sourceIndices[index]);
            return new CutGeometryAsset(hash, CoordinateSpace.DesktopUnityMillimetersV1, DesktopSurfaceRenderModelAdapter.Convert(source.bounds), RenderBuffer<Float3>.TakeOwnership(DesktopSurfaceRenderModelAdapter.Convert(source.vertices)), RenderBuffer<Float3>.TakeOwnership(DesktopSurfaceRenderModelAdapter.Convert(source.normals)), RenderBuffer<Float2>.TakeOwnership(DesktopSurfaceRenderModelAdapter.Convert(source.uv)), RenderBuffer<uint>.TakeOwnership(indices));
        }

        public static CutRenderResult CaptureResult(HBP.Core.Object3D.Cut source, ContractId cutId, ContractId interactionId, InteractionSequence sequence, ScopeRevision cutRevision, ScopeRevision renderRevision, StateRevision sourceStateRevision, RenderTemporalSample sample, CutGeometryAsset geometry, TextureAsset baseTexture, IEnumerable<CutOverlayFrame> overlays)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (geometry == null)
                throw new ArgumentNullException(nameof(geometry));
            if (baseTexture == null)
                throw new ArgumentNullException(nameof(baseTexture));
            Vector3 normal = source.Normal.normalized;
            Plane3F plane = new(DesktopSurfaceRenderModelAdapter.Convert(normal), -Vector3.Dot(normal, source.Point));
            return new CutRenderResult(cutId, interactionId, sequence, cutRevision, renderRevision, sourceStateRevision, sample, plane, geometry.Hash, Optional<CutGeometryAsset>.Some(geometry), baseTexture.Hash, Optional<TextureAsset>.Some(baseTexture), overlays);
        }

        private static Rgba32[] Convert(Color32[] values)
        {
            Rgba32[] result = new Rgba32[values.Length];
            for (int index = 0; index < values.Length; index++)
                result[index] = DesktopSurfaceRenderModelAdapter.Convert(values[index]);
            return result;
        }
    }
}
