using System;
using System.Collections.Generic;
using CRNL.HiBoP.Contracts;
using CRNL.HiBoP.Protocol;
using CRNL.HiBoP.RenderModel;
using CRNL.HiBoP.XR.Timeline.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace CRNL.HiBoP.XR.Cuts
{
    public sealed class P12CutResultRenderer : MonoBehaviour
    {
        private static readonly int CutTextureId = Shader.PropertyToID("_CutTexture");
        private static readonly int CutTimelineId = Shader.PropertyToID("_CutTimeline");
        private static readonly int CutTimelineIndexId = Shader.PropertyToID("_CutTimelineIndex");
        private static readonly int UseTimelineId = Shader.PropertyToID("_UseTimeline");
        private static readonly int TimelineInvariantId = Shader.PropertyToID("_TimelineInvariant");

        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private Material cutMaterial;
        [SerializeField] private P12CutGizmo gizmo;

        private MaterialPropertyBlock m_Properties;
        private AtomicCutResult<PreparedCut> m_Atomic;
        private SessionEpoch m_Session;
        private ContractId m_CutId;
        private ContractId m_ColumnId;

        public CutFeedbackState Feedback => m_Atomic?.Feedback ?? CutFeedbackState.Idle;
        public CutRenderResult CurrentResult { get; private set; }
        public bool HasCanonicalResult => CurrentResult != null;
        public bool UsesStableTimeline { get; private set; }

        public void Configure(MeshFilter filter, MeshRenderer renderer, Material material, P12CutGizmo cutGizmo)
        {
            meshFilter = filter;
            meshRenderer = renderer;
            cutMaterial = material;
            gizmo = cutGizmo;
        }

        public void BindContext(SessionEpoch session, ContractId cutId, ContractId columnId)
        {
            if (!session.IsValid || !cutId.IsValid || !columnId.IsValid)
                throw new ArgumentException("A valid session, cut and column are required.");
            Clear();
            m_Session = session;
            m_CutId = cutId;
            m_ColumnId = columnId;
            m_Atomic = new AtomicCutResult<PreparedCut>(session, cutId, Prepare, prepared => prepared.Dispose());
        }

        public bool ObserveRequest(Command command)
        {
            EnsureBound();
            bool accepted = m_Atomic.ObserveRequest(command);
            if (accepted)
                gizmo?.SetFeedback(CutFeedbackState.Pending);
            return accepted;
        }

        public CutResultApplyResult TryApply(CutResultPublication publication, out Exception error)
        {
            EnsureBound();
            CutResultApplyResult result = m_Atomic.TryPrepareAndCommit(publication, out error);
            if (result == CutResultApplyResult.Committed)
            {
                if (!m_Atomic.TryRead(out CommittedCutResult<PreparedCut> current))
                    throw new InvalidOperationException("The committed cut result disappeared.");
                CurrentResult = current.Publication.Result;
                ApplyPrepared(current.Prepared);
                gizmo?.ApplyCanonical(CurrentResult);
            }
            else if (result == CutResultApplyResult.Rejected)
            {
                gizmo?.SetFeedback(CutFeedbackState.Error);
            }

            return result;
        }

        public bool MarkError(ContractId interactionId, InteractionSequence sequence, Exception error)
        {
            EnsureBound();
            bool applied = m_Atomic.MarkError(interactionId, sequence, error);
            if (applied)
                gizmo?.SetFeedback(CutFeedbackState.Error);
            return applied;
        }

        public bool TryBindStableTimeline(PreloadedTimelineGpuResources resources)
        {
            EnsureBound();
            if (resources == null || !m_Atomic.TryRead(out CommittedCutResult<PreparedCut> current) || current.Publication.StablePlanTimeline == null || !ReferenceEquals(resources.Timeline, current.Publication.StablePlanTimeline))
                return false;
            PreloadedTimelineGpuResources.PreloadedGpuColumn column = FindColumn(resources, m_ColumnId);
            PreloadedTimelineGpuResources.PreloadedGpuCut cut = FindCut(column, m_CutId);
            if (cut == null)
                return false;

            m_Properties ??= new MaterialPropertyBlock();
            meshRenderer.GetPropertyBlock(m_Properties);
            m_Properties.SetTexture(CutTimelineId, cut.Texture);
            m_Properties.SetBuffer(CutTimelineIndexId, resources.SelectionBuffer);
            m_Properties.SetFloat(TimelineInvariantId, cut.Texture.depth == 1 ? 1f : 0f);
            m_Properties.SetFloat(UseTimelineId, 1f);
            meshRenderer.SetPropertyBlock(m_Properties);
            UsesStableTimeline = true;
            return true;
        }

        public void Clear()
        {
            if (m_Atomic != null && m_Session.IsValid && m_CutId.IsValid)
                m_Atomic.Reset(m_Session, m_CutId);
            m_Atomic = null;
            CurrentResult = null;
            UsesStableTimeline = false;
            if (meshFilter != null)
                meshFilter.sharedMesh = null;
            if (meshRenderer != null)
            {
                meshRenderer.enabled = false;
                meshRenderer.SetPropertyBlock(null);
            }
        }

        private PreparedCut Prepare(CutResultPublication publication)
        {
            ValidateReferences();
            CutGeometryLease geometry = CutGeometryCache.Acquire(publication.Result.GeometryHash, publication.Result.Geometry);
            CutTextureLease baseTexture = null;
            Texture2D currentImage = null;
            try
            {
                baseTexture = CutTextureCache.Acquire(publication.Result.BaseTextureHash, publication.Result.BaseTexture, publication.Manifest.Width, publication.Manifest.Height);
                CutOverlayFrame overlay = FindOverlay(publication.Result, m_ColumnId);
                if (overlay != null)
                    currentImage = CreateTexture(overlay.Width, overlay.Height, overlay.Pixels, false, "P12 final Desktop cut image");
                return new PreparedCut(geometry, baseTexture, currentImage, overlay != null);
            }
            catch
            {
                if (currentImage != null)
                    DestroyUnityObject(currentImage);
                baseTexture?.Dispose();
                geometry.Dispose();
                throw;
            }
        }

        private void ApplyPrepared(PreparedCut prepared)
        {
            meshFilter.sharedMesh = prepared.Geometry.Mesh;
            meshRenderer.sharedMaterial = cutMaterial;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            meshRenderer.enabled = true;
            m_Properties ??= new MaterialPropertyBlock();
            m_Properties.Clear();
            m_Properties.SetTexture(CutTextureId, prepared.CurrentImage ?? prepared.BaseTexture.Texture);
            m_Properties.SetFloat(UseTimelineId, 0f);
            meshRenderer.SetPropertyBlock(m_Properties);
            UsesStableTimeline = false;
        }

        private void OnDestroy() => Clear();

        private void EnsureBound()
        {
            if (m_Atomic == null)
                throw new InvalidOperationException("Bind the cut renderer to a canonical context first.");
        }

        private void ValidateReferences()
        {
            if (meshFilter == null || meshRenderer == null || cutMaterial == null)
                throw new InvalidOperationException("The P12 renderer references must be serialized in its prefab.");
        }

        private static CutOverlayFrame FindOverlay(CutRenderResult result, ContractId columnId)
        {
            for (int index = 0; index < result.Overlays.Count; index++)
            {
                if (result.Overlays[index].ColumnId == columnId)
                    return result.Overlays[index];
            }

            return null;
        }

        private static PreloadedTimelineGpuResources.PreloadedGpuColumn FindColumn(PreloadedTimelineGpuResources resources, ContractId columnId)
        {
            for (int index = 0; index < resources.Columns.Count; index++)
            {
                if (resources.Columns[index].ColumnId == columnId)
                    return resources.Columns[index];
            }

            return null;
        }

        private static PreloadedTimelineGpuResources.PreloadedGpuCut FindCut(PreloadedTimelineGpuResources.PreloadedGpuColumn column, ContractId cutId)
        {
            if (column == null)
                return null;
            for (int index = 0; index < column.Cuts.Count; index++)
            {
                if (column.Cuts[index].CutId == cutId)
                    return column.Cuts[index];
            }

            return null;
        }

        private static Texture2D CreateTexture(int width, int height, RenderBuffer<Rgba32> pixels, bool linear, string name)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false, linear)
            {
                name = name,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            texture.SetPixelData(pixels.ToArray(), 0);
            texture.Apply(false, true);
            return texture;
        }

        private static Mesh CreateMesh(CutGeometryAsset asset)
        {
            if (asset.CoordinateSpace.Handedness != CoordinateHandedness.Left || asset.CoordinateSpace.AxisOrder != CoordinateAxisOrder.Xyz || asset.CoordinateSpace.Unit != LengthUnit.Millimeter || asset.CoordinateSpace.MappingVersion != 1)
                throw new ArgumentException("P12 accepts only the canonical P03 cut coordinate space.", nameof(asset));
            int count = asset.Positions.Count;
            var positions = new Vector3[count];
            var normals = new Vector3[count];
            var uvs = new Vector2[count];
            float scale = asset.CoordinateSpace.MetersPerUnit;
            for (int index = 0; index < count; index++)
            {
                Float3 position = asset.Positions[index];
                Float3 normal = asset.Normals[index];
                Float2 uv = asset.Uvs[index];
                positions[index] = new Vector3(position.X, position.Y, position.Z) * scale;
                normals[index] = new Vector3(normal.X, normal.Y, normal.Z);
                uvs[index] = new Vector2(uv.X, uv.Y);
            }

            var indices = new int[asset.Indices.Count];
            for (int index = 0; index < indices.Length; index++)
                indices[index] = checked((int)asset.Indices[index]);
            var mesh = new Mesh { name = "P12 Desktop cut", indexFormat = count <= ushort.MaxValue ? IndexFormat.UInt16 : IndexFormat.UInt32 };
            mesh.vertices = positions;
            mesh.normals = normals;
            mesh.uv = uvs;
            mesh.SetIndices(indices, MeshTopology.Triangles, 0, true);
            mesh.UploadMeshData(true);
            return mesh;
        }

        private static void DestroyUnityObject(UnityEngine.Object value)
        {
            if (value == null)
                return;
            if (Application.isPlaying)
                Destroy(value);
            else
                DestroyImmediate(value);
        }

        private sealed class PreparedCut : IDisposable
        {
            public PreparedCut(CutGeometryLease geometry, CutTextureLease baseTexture, Texture2D currentImage, bool usesOverlay)
            {
                Geometry = geometry;
                BaseTexture = baseTexture;
                CurrentImage = currentImage;
                UsesOverlay = usesOverlay;
            }

            public CutGeometryLease Geometry { get; }
            public CutTextureLease BaseTexture { get; }
            public Texture2D CurrentImage { get; }
            public bool UsesOverlay { get; }

            public void Dispose()
            {
                if (CurrentImage != null)
                    DestroyUnityObject(CurrentImage);
                BaseTexture.Dispose();
                Geometry.Dispose();
            }
        }

        private sealed class CutGeometryLease : IDisposable
        {
            private AssetHash m_Hash;

            public CutGeometryLease(AssetHash hash, Mesh mesh)
            {
                m_Hash = hash;
                Mesh = mesh;
            }

            public Mesh Mesh { get; }

            public void Dispose()
            {
                if (!m_Hash.IsValid) return;
                CutGeometryCache.Release(m_Hash);
                m_Hash = default;
            }
        }

        private static class CutGeometryCache
        {
            private static readonly Dictionary<AssetHash, CacheEntry<Mesh>> Entries = new();

            public static CutGeometryLease Acquire(AssetHash hash, Optional<CutGeometryAsset> inline)
            {
                if (Entries.TryGetValue(hash, out CacheEntry<Mesh> entry))
                {
                    entry.References++;
                    return new CutGeometryLease(hash, entry.Value);
                }

                if (!inline.HasValue)
                    throw new InvalidOperationException("The content-addressed cut geometry is not resident and was not supplied by Desktop.");
                Mesh mesh = CreateMesh(inline.Value);
                Entries.Add(hash, new CacheEntry<Mesh>(mesh));
                return new CutGeometryLease(hash, mesh);
            }

            public static void Release(AssetHash hash)
            {
                CacheEntry<Mesh> entry = Entries[hash];
                if (--entry.References > 0) return;
                Entries.Remove(hash);
                DestroyUnityObject(entry.Value);
            }
        }

        private sealed class CutTextureLease : IDisposable
        {
            private AssetHash m_Hash;

            public CutTextureLease(AssetHash hash, Texture2D texture)
            {
                m_Hash = hash;
                Texture = texture;
            }

            public Texture2D Texture { get; }

            public void Dispose()
            {
                if (!m_Hash.IsValid) return;
                CutTextureCache.Release(m_Hash);
                m_Hash = default;
            }
        }

        private static class CutTextureCache
        {
            private static readonly Dictionary<AssetHash, CacheEntry<Texture2D>> Entries = new();

            public static CutTextureLease Acquire(AssetHash hash, Optional<TextureAsset> inline, int width, int height)
            {
                if (Entries.TryGetValue(hash, out CacheEntry<Texture2D> entry))
                {
                    entry.References++;
                    return new CutTextureLease(hash, entry.Value);
                }

                if (!inline.HasValue)
                    throw new InvalidOperationException("The content-addressed cut base texture is not resident and was not supplied by Desktop.");
                TextureAsset asset = inline.Value;
                if (asset.Width != width || asset.Height != height || asset.ColorSpace != TextureColorSpace.Srgb)
                    throw new ArgumentException("The Desktop base texture does not match the P12 manifest.", nameof(inline));
                Texture2D texture = CreateTexture(width, height, asset.Pixels, false, "P12 Desktop cut base");
                Entries.Add(hash, new CacheEntry<Texture2D>(texture));
                return new CutTextureLease(hash, texture);
            }

            public static void Release(AssetHash hash)
            {
                CacheEntry<Texture2D> entry = Entries[hash];
                if (--entry.References > 0) return;
                Entries.Remove(hash);
                DestroyUnityObject(entry.Value);
            }
        }

        private sealed class CacheEntry<T> where T : UnityEngine.Object
        {
            public CacheEntry(T value)
            {
                Value = value;
                References = 1;
            }

            public T Value { get; }
            public int References { get; set; }
        }
    }
}
