using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using CRNL.HiBoP.Contracts;
using CRNL.HiBoP.RenderModel;
using UnityEngine;
using UnityEngine.Rendering;

namespace CRNL.HiBoP.XR.Sites
{
    public sealed class P10SiteRenderer : MonoBehaviour, IDisposable
    {
        public const float MinimumRayRadiusMillimeters = 2f;
        public const float ProximityThresholdMillimeters = 12f;

        private static readonly int PositionsId = Shader.PropertyToID("_P10SitePositions");
        private static readonly int DynamicsId = Shader.PropertyToID("_P10SiteDynamics");
        private static readonly int LocalToWorldId = Shader.PropertyToID("_P10SiteLocalToWorld");

        [SerializeField] private Mesh siteMesh;
        [SerializeField] private Material siteMaterial;

        private readonly int[] m_FeedbackIndices = { -1, -1, -1 };
        private SiteAssetRuntimeCache.SiteAssetLease m_AssetLease;
        private GraphicsBuffer m_DynamicBuffer;
        private MaterialPropertyBlock m_Properties;
        private SiteDynamic[] m_Dynamics;
        private float[] m_Radii;
        private float[] m_NodeMaximumRadii;
        private byte[] m_Visibility;
        private float m_MaximumRadiusMillimeters = MinimumRayRadiusMillimeters;
        private bool m_HasFrame;
        private bool m_StaticPositionsValidated;

        public SiteAsset SiteAsset => m_AssetLease?.Asset;

        public int SiteCount => m_Dynamics?.Length ?? 0;

        public int ExpectedDrawCalls => m_HasFrame && isActiveAndEnabled ? 1 : 0;

        public long DynamicBufferBytes => (long)SiteCount * SiteDynamic.Stride;

        internal GraphicsBuffer SharedPositionBuffer => m_AssetLease?.PositionBuffer;

        internal float MaximumRadiusMillimeters => m_MaximumRadiusMillimeters;

        internal bool StaticPositionsValidated => m_StaticPositionsValidated;

        public void Configure(Mesh mesh, Material material)
        {
            siteMesh = mesh;
            siteMaterial = material;
        }

        public void SetAsset(SiteAsset asset)
        {
            ValidateSerializedReferences();
            SiteAssetRuntimeCache.SiteAssetLease next = SiteAssetRuntimeCache.Acquire(asset);
            Clear();
            m_AssetLease = next;
            int count = asset.SiteIds.Count;
            m_Dynamics = new SiteDynamic[count];
            m_Radii = new float[count];
            m_Visibility = new byte[count];
            m_DynamicBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, count, SiteDynamic.Stride);
            m_DynamicBuffer.SetData(m_Dynamics);
            m_NodeMaximumRadii = m_AssetLease.Index.CreateDynamicRadiusBounds(m_Radii, m_Visibility);
            m_Properties ??= new MaterialPropertyBlock();
            m_MaximumRadiusMillimeters = MinimumRayRadiusMillimeters;
            m_StaticPositionsValidated = false;
        }

        public void ApplyFrame(SiteRenderFrame frame, IReadOnlyList<SiteDirtyRange> dirtyRanges = null)
        {
            if (frame == null)
                throw new ArgumentNullException(nameof(frame));
            if (m_AssetLease == null)
                throw new InvalidOperationException("A SiteAsset must be active before a frame is applied.");
            if (frame.SiteAssetHash != SiteAsset.Hash || frame.SiteCount != SiteCount)
                throw new ArgumentException("The site frame does not match the active SiteAsset hash and count.", nameof(frame));
            if (!m_HasFrame && dirtyRanges != null)
                throw new InvalidOperationException("The first frame for a SiteAsset must be uploaded in full.");

            ValidateImmutablePositions(frame);

            if (dirtyRanges == null)
            {
                m_MaximumRadiusMillimeters = MinimumRayRadiusMillimeters;
                ApplyRange(frame, 0, SiteCount);
                m_NodeMaximumRadii = m_AssetLease.Index.CreateDynamicRadiusBounds(m_Radii, m_Visibility);
            }
            else
            {
                ValidateDirtyRanges(dirtyRanges, SiteCount);
                bool maximumMayShrink = false;
                for (int rangeIndex = 0; rangeIndex < dirtyRanges.Count; rangeIndex++)
                {
                    SiteDirtyRange range = dirtyRanges[rangeIndex];
                    int end = checked(range.Start + range.Count);
                    for (int index = range.Start; index < end; index++)
                    {
                        if (m_Radii[index] >= m_MaximumRadiusMillimeters && frame.Sizes[index] < m_Radii[index])
                            maximumMayShrink = true;
                    }
                }

                for (int rangeIndex = 0; rangeIndex < dirtyRanges.Count; rangeIndex++)
                {
                    SiteDirtyRange range = dirtyRanges[rangeIndex];
                    ApplyRange(frame, range.Start, range.Count);
                    m_AssetLease.Index.UpdateDynamicRadiusBounds(m_NodeMaximumRadii, m_Radii, m_Visibility, range.Start, range.Count);
                }

                if (maximumMayShrink)
                    RecalculateMaximumRadius();
            }

            m_HasFrame = true;
        }

        public bool Raycast(Ray worldRay, float maximumWorldDistanceMeters, out SitePickResult result)
        {
            if (!m_HasFrame)
            {
                result = SitePickResult.None;
                return false;
            }

            if (!IsFinite(maximumWorldDistanceMeters) || maximumWorldDistanceMeters <= 0f)
                throw new ArgumentOutOfRangeException(nameof(maximumWorldDistanceMeters));

            Vector3 localOriginMillimeters = transform.InverseTransformPoint(worldRay.origin) * 1000f;
            Vector3 localDirection = transform.InverseTransformVector(worldRay.direction);
            if (localDirection.sqrMagnitude <= 0f)
                throw new ArgumentOutOfRangeException(nameof(worldRay));
            localDirection.Normalize();
            float maximumLocalDistanceMillimeters = transform.InverseTransformVector(worldRay.direction.normalized * maximumWorldDistanceMeters).magnitude * 1000f;
            result = m_AssetLease.Index.Raycast(localOriginMillimeters, localDirection, maximumLocalDistanceMillimeters, MinimumRayRadiusMillimeters, m_Radii, m_Visibility, m_NodeMaximumRadii);
            return result.Hit;
        }

        public bool FindNearest(Vector3 worldPoint, out SitePickResult result)
        {
            if (!m_HasFrame)
            {
                result = SitePickResult.None;
                return false;
            }

            Vector3 localPointMillimeters = transform.InverseTransformPoint(worldPoint) * 1000f;
            result = m_AssetLease.Index.FindNearest(localPointMillimeters, ProximityThresholdMillimeters, m_Radii, m_Visibility, m_NodeMaximumRadii);
            return result.Hit;
        }

        public bool TryGetIndex(ContractId siteId, out int index)
        {
            index = -1;
            return m_AssetLease != null && m_AssetLease.Index.TryGetIndex(siteId, out index);
        }

        public void SetFeedback(int hoverIndex, int pendingIndex, int canonicalIndex)
        {
            if (m_Dynamics == null)
            {
                for (int index = 0; index < m_FeedbackIndices.Length; index++)
                    m_FeedbackIndices[index] = -1;
                return;
            }

            ValidateOptionalIndex(hoverIndex);
            ValidateOptionalIndex(pendingIndex);
            ValidateOptionalIndex(canonicalIndex);
            int[] next = { hoverIndex, pendingIndex, canonicalIndex };
            var changed = new HashSet<int>();
            for (int slot = 0; slot < m_FeedbackIndices.Length; slot++)
            {
                if (m_FeedbackIndices[slot] >= 0)
                    changed.Add(m_FeedbackIndices[slot]);
                if (next[slot] >= 0)
                    changed.Add(next[slot]);
                m_FeedbackIndices[slot] = next[slot];
            }

            foreach (int index in changed)
            {
                uint feedback = 0;
                if (index == hoverIndex)
                    feedback |= 1u;
                if (index == pendingIndex)
                    feedback |= 2u;
                if (index == canonicalIndex)
                    feedback |= 4u;
                SiteDynamic value = m_Dynamics[index];
                value.Feedback = feedback;
                m_Dynamics[index] = value;
                m_DynamicBuffer.SetData(m_Dynamics, index, index, 1);
            }
        }

        public void RenderNow(Camera camera = null)
        {
            if (!m_HasFrame || !isActiveAndEnabled)
                return;
            ValidateSerializedReferences();
            m_Properties.Clear();
            m_Properties.SetBuffer(PositionsId, m_AssetLease.PositionBuffer);
            m_Properties.SetBuffer(DynamicsId, m_DynamicBuffer);
            m_Properties.SetMatrix(LocalToWorldId, transform.localToWorldMatrix);
            var renderParams = new RenderParams(siteMaterial)
            {
                camera = camera,
                layer = gameObject.layer,
                matProps = m_Properties,
                motionVectorMode = MotionVectorGenerationMode.ForceNoMotion,
                receiveShadows = false,
                shadowCastingMode = ShadowCastingMode.Off,
                worldBounds = CalculateWorldBounds(),
            };
            Graphics.RenderMeshPrimitives(renderParams, siteMesh, 0, SiteCount);
        }

        public void Clear()
        {
            m_DynamicBuffer?.Dispose();
            m_DynamicBuffer = null;
            m_AssetLease?.Dispose();
            m_AssetLease = null;
            m_Dynamics = null;
            m_Radii = null;
            m_NodeMaximumRadii = null;
            m_Visibility = null;
            m_MaximumRadiusMillimeters = MinimumRayRadiusMillimeters;
            m_HasFrame = false;
            m_StaticPositionsValidated = false;
            for (int index = 0; index < m_FeedbackIndices.Length; index++)
                m_FeedbackIndices[index] = -1;
        }

        public void Dispose() => Clear();

        private void LateUpdate() => RenderNow();

        private void OnDestroy() => Clear();

        private void ApplyRange(SiteRenderFrame frame, int start, int count)
        {
            int end = checked(start + count);
            for (int index = start; index < end; index++)
            {
                float radius = frame.Sizes[index];
                byte visible = frame.Visibility[index];
                m_Radii[index] = radius;
                m_Visibility[index] = visible;
                m_MaximumRadiusMillimeters = Mathf.Max(m_MaximumRadiusMillimeters, radius);
                m_Dynamics[index] = new SiteDynamic(radius, Pack(frame.Colors[index]), visible | ((uint)frame.Flags[index] << 8), m_Dynamics[index].Feedback);
            }

            m_DynamicBuffer.SetData(m_Dynamics, start, start, count);
        }

        private void ValidateImmutablePositions(SiteRenderFrame frame)
        {
            // Positions are uploaded only from the content-addressed SiteAsset. Validate the
            // first complete frame as a pipeline guard, then trust the already-validated asset
            // hash so streaming frames are neither retained nor scanned in O(N).
            if (m_StaticPositionsValidated)
                return;
            for (int index = 0; index < SiteCount; index++)
            {
                if (!frame.Positions[index].Equals(SiteAsset.Positions[index]))
                    throw new ArgumentException("Site positions are immutable for a SiteAsset hash.", nameof(frame));
            }

            m_StaticPositionsValidated = true;
        }

        private void RecalculateMaximumRadius()
        {
            float maximum = MinimumRayRadiusMillimeters;
            for (int index = 0; index < m_Radii.Length; index++)
                maximum = Mathf.Max(maximum, m_Radii[index]);
            m_MaximumRadiusMillimeters = maximum;
        }

        private Bounds CalculateWorldBounds()
        {
            Bounds3F source = SiteAsset.Bounds;
            Vector3 minimum = new(source.Minimum.X, source.Minimum.Y, source.Minimum.Z);
            Vector3 maximum = new(source.Maximum.X, source.Maximum.Y, source.Maximum.Z);
            Vector3 center = (minimum + maximum) * 0.5f * 0.001f;
            Vector3 extents = (maximum - minimum) * 0.5f * 0.001f + Vector3.one * (m_MaximumRadiusMillimeters * 0.001f);
            Vector3 worldCenter = transform.TransformPoint(center);
            var result = new Bounds(worldCenter, Vector3.zero);
            for (int x = -1; x <= 1; x += 2)
            for (int y = -1; y <= 1; y += 2)
            for (int z = -1; z <= 1; z += 2)
                result.Encapsulate(transform.TransformPoint(center + Vector3.Scale(extents, new Vector3(x, y, z))));
            return result;
        }

        private void ValidateOptionalIndex(int index)
        {
            if (index < -1 || index >= SiteCount)
                throw new ArgumentOutOfRangeException(nameof(index));
        }

        private void ValidateSerializedReferences()
        {
            if (siteMesh == null || siteMaterial == null)
                throw new InvalidOperationException("The P10 site mesh and material must be serialized in the prefab.");
        }

        private static void ValidateDirtyRanges(IReadOnlyList<SiteDirtyRange> ranges, int siteCount)
        {
            if (ranges.Count == 0)
                throw new ArgumentException("At least one dirty range is required.", nameof(ranges));
            int previousEnd = 0;
            for (int index = 0; index < ranges.Count; index++)
            {
                SiteDirtyRange range = ranges[index];
                int end = checked(range.Start + range.Count);
                if (end > siteCount || (index > 0 && range.Start < previousEnd))
                    throw new ArgumentException("Dirty ranges must be ordered, non-overlapping and inside the frame.", nameof(ranges));
                previousEnd = end;
            }
        }

        private static uint Pack(Rgba32 color) => color.R | ((uint)color.G << 8) | ((uint)color.B << 16) | ((uint)color.A << 24);

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        [StructLayout(LayoutKind.Sequential)]
        private struct SiteDynamic
        {
            public const int Stride = 16;

            public SiteDynamic(float radiusMillimeters, uint color, uint state, uint feedback)
            {
                RadiusMillimeters = radiusMillimeters;
                Color = color;
                State = state;
                Feedback = feedback;
            }

            public float RadiusMillimeters;
            public uint Color;
            public uint State;
            public uint Feedback;
        }
    }
}
