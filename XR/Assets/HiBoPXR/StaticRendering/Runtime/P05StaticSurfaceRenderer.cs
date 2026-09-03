using System;
using CRNL.HiBoP.RenderModel;
using UnityEngine;
using UnityEngine.Rendering;

namespace CRNL.HiBoP.XR.StaticRendering
{
    public enum SurfaceTransparency
    {
        Opaque,
        Transparent,
    }

    public sealed class P05StaticSurfaceRenderer : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private Material opaqueMaterial;
        [SerializeField] private Material transparentMaterial;
        [SerializeField] private MeshFilter transparentDepthFilter;
        [SerializeField] private MeshRenderer transparentDepthRenderer;
        [SerializeField] private Material transparentDepthMaterial;
        [SerializeField] private Color color = new(0.72f, 0.72f, 0.74f, 1f);
        [SerializeField, Range(0f, 1f)] private float transparentAlpha = 0.42f;
        [SerializeField] private int transparentSortingOrder;

        private MaterialPropertyBlock m_Properties;
        private SurfaceMeshLease m_Lease;

        public SurfaceAsset SurfaceAsset { get; private set; }

        public void Configure(MeshFilter filter, MeshRenderer renderer, Material opaque, Material transparent, MeshFilter depthFilter, MeshRenderer depthRenderer, Material depthMaterial, Color surfaceColor, float alpha, int sortingOrder)
        {
            meshFilter = filter;
            meshRenderer = renderer;
            opaqueMaterial = opaque;
            transparentMaterial = transparent;
            transparentDepthFilter = depthFilter;
            transparentDepthRenderer = depthRenderer;
            transparentDepthMaterial = depthMaterial;
            color = surfaceColor;
            transparentAlpha = Mathf.Clamp01(alpha);
            transparentSortingOrder = sortingOrder;
        }

        public void SetSurface(SurfaceAsset asset, SurfaceTransparency transparency)
        {
            ValidateReferences(transparency);
            SurfaceMeshLease nextLease = SurfaceMeshCache.Acquire(asset);
            Clear();
            m_Lease = nextLease;
            SurfaceAsset = asset;
            meshFilter.sharedMesh = nextLease.Mesh;
            ApplyMaterial(transparency);
        }

        public void Clear()
        {
            if (meshFilter != null)
            {
                meshFilter.sharedMesh = null;
            }

            if (transparentDepthFilter != null)
            {
                transparentDepthFilter.sharedMesh = null;
            }

            if (transparentDepthRenderer != null)
            {
                transparentDepthRenderer.enabled = false;
            }

            m_Lease?.Dispose();
            m_Lease = null;
            SurfaceAsset = null;
        }

        private void ApplyMaterial(SurfaceTransparency transparency)
        {
            bool isTransparent = transparency == SurfaceTransparency.Transparent;
            Material material = isTransparent ? transparentMaterial : opaqueMaterial;
            if (material == null)
            {
                throw new InvalidOperationException($"The {transparency} P05 material is not assigned.");
            }

            meshRenderer.sharedMaterial = material;
            meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;
            meshRenderer.lightProbeUsage = LightProbeUsage.Off;
            meshRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            meshRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            meshRenderer.sortingOrder = isTransparent ? transparentSortingOrder : 0;

            if (isTransparent)
            {
                if (transparentDepthFilter == null || transparentDepthRenderer == null || transparentDepthMaterial == null)
                {
                    throw new InvalidOperationException("The P05 transparent depth-prepass references are not assigned.");
                }

                transparentDepthFilter.sharedMesh = m_Lease.Mesh;
                transparentDepthRenderer.sharedMaterial = transparentDepthMaterial;
                transparentDepthRenderer.shadowCastingMode = ShadowCastingMode.Off;
                transparentDepthRenderer.receiveShadows = false;
                transparentDepthRenderer.lightProbeUsage = LightProbeUsage.Off;
                transparentDepthRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                transparentDepthRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
                transparentDepthRenderer.sortingOrder = transparentSortingOrder;
                transparentDepthRenderer.enabled = true;
            }
            else if (transparentDepthRenderer != null)
            {
                transparentDepthFilter.sharedMesh = null;
                transparentDepthRenderer.enabled = false;
            }

            Color appliedColor = color;
            appliedColor.a = isTransparent ? transparentAlpha : 1f;
            m_Properties ??= new MaterialPropertyBlock();
            m_Properties.Clear();
            m_Properties.SetColor(BaseColorId, appliedColor);
            meshRenderer.SetPropertyBlock(m_Properties);
        }

        private void OnDestroy()
        {
            Clear();
        }

        private void ValidateReferences(SurfaceTransparency transparency)
        {
            if (meshFilter == null || meshRenderer == null)
            {
                throw new InvalidOperationException("P05 renderer references must be serialized in its prefab.");
            }

            if (transparency == SurfaceTransparency.Opaque && opaqueMaterial == null)
            {
                throw new InvalidOperationException("The Opaque P05 material is not assigned.");
            }

            if (transparency == SurfaceTransparency.Transparent && (transparentMaterial == null || transparentDepthFilter == null || transparentDepthRenderer == null || transparentDepthMaterial == null))
            {
                throw new InvalidOperationException("The P05 transparent material and depth-prepass references are not assigned.");
            }
        }
    }
}
