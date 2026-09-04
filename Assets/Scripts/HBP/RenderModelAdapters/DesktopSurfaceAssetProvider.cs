using System;
using System.Collections.Generic;
using CRNL.HiBoP.Contracts;
using CRNL.HiBoP.Protocol;
using CRNL.HiBoP.RenderModel;

namespace HBP.RenderModelAdapters
{
    public sealed class DesktopSurfaceAssetProvider : IDisposable
    {
        private readonly InMemoryRemoteAssetProvider m_Provider = new();

        public InMemoryRemoteAssetProvider Provider => m_Provider;

        public RemoteAssetDescriptor Publish(SurfaceAsset asset, ContractId assetId, int chunkBytes, AssetHash anatomicalBaseHash = default)
        {
            if (asset == null)
                throw new ArgumentNullException(nameof(asset));

            byte[] payload = SurfaceAssetPayloadCodec.Encode(asset);
            AssetHash contentHash = SurfaceAssetPayloadCodec.ComputeHash(payload);
            RemoteAssetVariant variant;
            IReadOnlyList<RemoteAssetDependency> dependencies;
            if (asset.Representation == SurfaceRepresentation.Anatomical)
            {
                variant = RemoteAssetVariant.Anatomical;
                dependencies = Array.Empty<RemoteAssetDependency>();
            }
            else if (asset.Representation == SurfaceRepresentation.Inflated)
            {
                if (!anatomicalBaseHash.IsValid)
                    throw new ArgumentException("An inflated surface requires its anatomical variant hash.", nameof(anatomicalBaseHash));
                variant = RemoteAssetVariant.Inflated;
                dependencies = new[] { new RemoteAssetDependency(RemoteAssetDependencyKind.VariantBase, anatomicalBaseHash) };
            }
            else
            {
                variant = RemoteAssetVariant.None;
                dependencies = Array.Empty<RemoteAssetDependency>();
            }

            var descriptor = new RemoteAssetDescriptor(new AssetReference(assetId, contentHash, SurfaceAssetPayloadCodec.SchemaVersion), RemoteAssetKind.Surface, variant, payload.Length, asset.Positions.Count, asset.Indices.Count, asset.StaticUvs.Count, chunkBytes, dependencies);
            try
            {
                m_Provider.Publish(descriptor, payload);
                return descriptor;
            }
            finally
            {
                Array.Clear(payload, 0, payload.Length);
            }
        }

        public void Dispose()
        {
            m_Provider.Dispose();
        }
    }
}
