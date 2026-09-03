using System;
using CRNL.HiBoP.Contracts;
using CRNL.HiBoP.RenderModel;
using CRNL.HiBoP.XR.RemoteAssets;
using CRNL.HiBoP.XR.StaticRendering;
using UnityEngine;

namespace CRNL.HiBoP.XR.BrainInstances
{
    public sealed class BrainInstanceView : MonoBehaviour, IDisposable
    {
        [SerializeField] private P05StaticSurfaceRenderer surfaceRenderer;

        private RemoteSurfaceRendererBinding m_SurfaceBinding;

        public SurfaceAsset SurfaceAsset => m_SurfaceBinding?.ActiveSurfaceAsset;

        public AssetHash SurfaceHash => m_SurfaceBinding?.ActiveHash ?? default;

        public Mesh SharedMesh => surfaceRenderer == null ? null : surfaceRenderer.SharedMesh;

        public int ExpectedDrawCalls { get; private set; }

        public void Configure(P05StaticSurfaceRenderer renderer)
        {
            surfaceRenderer = renderer;
        }

        internal void Initialize(RemoteSurfaceAssetStore store)
        {
            if (m_SurfaceBinding != null)
                throw new InvalidOperationException("The BrainInstance view is already initialized.");
            if (surfaceRenderer == null)
                throw new InvalidOperationException("The BrainInstance prefab is missing its serialized P05 renderer.");
            m_SurfaceBinding = new RemoteSurfaceRendererBinding(store, surfaceRenderer);
        }

        internal bool TryActivate(ResolvedBrainBinding resolved)
        {
            if (m_SurfaceBinding == null)
                throw new InvalidOperationException("The BrainInstance view is not initialized.");
            if (!m_SurfaceBinding.TryActivate(resolved.SurfaceHash, resolved.Transparency, resolved.Representation))
                return false;
            ExpectedDrawCalls = resolved.Transparency == SurfaceTransparency.Transparent ? 2 : 1;
            return true;
        }

        internal void ApplyLayout(BrainInstanceLayout layout)
        {
            transform.localPosition = layout.LocalPosition;
            transform.localRotation = layout.LocalRotation;
            transform.localScale = Vector3.one * layout.UniformScale;
            gameObject.SetActive(layout.Visible);
        }

        public void Dispose()
        {
            m_SurfaceBinding?.Dispose();
            m_SurfaceBinding = null;
            ExpectedDrawCalls = 0;
        }

        private void OnDestroy()
        {
            Dispose();
        }
    }
}
