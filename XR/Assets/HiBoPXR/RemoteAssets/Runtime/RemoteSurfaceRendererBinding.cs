using System;
using CRNL.HiBoP.Contracts;
using CRNL.HiBoP.RenderModel;
using CRNL.HiBoP.XR.StaticRendering;

namespace CRNL.HiBoP.XR.RemoteAssets
{
    public sealed class RemoteSurfaceRendererBinding : IDisposable
    {
        private readonly RemoteSurfaceAssetStore m_Store;
        private readonly P05StaticSurfaceRenderer m_Renderer;
        private RemoteSurfaceAssetLease m_Lease;

        public RemoteSurfaceRendererBinding(RemoteSurfaceAssetStore store, P05StaticSurfaceRenderer renderer)
        {
            m_Store = store ?? throw new ArgumentNullException(nameof(store));
            m_Renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        }

        public AssetHash ActiveHash => m_Lease?.Hash ?? default;

        public SurfaceAsset ActiveSurfaceAsset => m_Lease?.Asset;

        public bool TryActivate(AssetHash hash, SurfaceTransparency transparency)
        {
            if (!m_Store.TryAcquire(hash, out RemoteSurfaceAssetLease nextLease))
                return false;

            try
            {
                m_Renderer.SetSurface(nextLease.Asset, transparency);
                RemoteSurfaceAssetLease previousLease = m_Lease;
                m_Lease = nextLease;
                previousLease?.Dispose();
                return true;
            }
            catch
            {
                nextLease.Dispose();
                throw;
            }
        }

        public void ReleaseActiveContent()
        {
            m_Renderer.Clear();
            m_Lease?.Dispose();
            m_Lease = null;
        }

        public void Dispose()
        {
            ReleaseActiveContent();
        }
    }
}
