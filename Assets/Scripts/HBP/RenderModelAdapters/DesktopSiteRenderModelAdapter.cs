using System;
using System.Collections.Generic;
using CRNL.HiBoP.Contracts;
using CRNL.HiBoP.RenderModel;
using HBP.Core.Object3D;
using HBP.Data.Module3D;
using UnityEngine;

namespace HBP.RenderModelAdapters
{
    public static class DesktopSiteRenderModelAdapter
    {
        public static SiteAsset CaptureAsset(IReadOnlyList<Site> sites, IReadOnlyList<ContractId> siteIds, AssetHash hash)
        {
            if (sites == null)
                throw new ArgumentNullException(nameof(sites));
            if (siteIds == null)
                throw new ArgumentNullException(nameof(siteIds));
            if (sites.Count == 0 || sites.Count != siteIds.Count)
                throw new ArgumentException("Sites and opaque IDs must have the same non-zero count.");

            ContractId[] ids = new ContractId[sites.Count];
            Float3[] positions = new Float3[sites.Count];
            Vector3 minimum = new(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            Vector3 maximum = new(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
            for (int index = 0; index < sites.Count; index++)
            {
                Site site = sites[index] ?? throw new ArgumentException("Sites cannot contain null.", nameof(sites));
                ids[index] = siteIds[index];
                Vector3 position = site.Information != null ? site.Information.DefaultPosition : site.transform.localPosition;
                positions[index] = DesktopSurfaceRenderModelAdapter.Convert(position);
                minimum = Vector3.Min(minimum, position);
                maximum = Vector3.Max(maximum, position);
            }

            return new SiteAsset(hash, CoordinateSpace.DesktopUnityMillimetersV1, new Bounds3F(DesktopSurfaceRenderModelAdapter.Convert(minimum), DesktopSurfaceRenderModelAdapter.Convert(maximum)), RenderBuffer<ContractId>.TakeOwnership(ids), RenderBuffer<Float3>.TakeOwnership(positions));
        }

        public static SiteRenderFrame CaptureFrame(Column3DDynamic source, AssetHash siteAssetHash, StateRevision sourceStateRevision)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (source.Sites == null || source.Sites.Count == 0)
                throw new InvalidOperationException("The column has no sites.");

            int count = source.Sites.Count;
            Float3[] positions = new Float3[count];
            Rgba32[] colors = new Rgba32[count];
            float[] sizes = new float[count];
            byte[] visibility = new byte[count];
            SiteRenderFlags[] flags = new SiteRenderFlags[count];
            for (int index = 0; index < count; index++)
            {
                Site site = source.Sites[index];
                Renderer renderer = site.GetComponent<Renderer>();
                Color32 color = renderer != null && renderer.sharedMaterial != null ? renderer.sharedMaterial.color : site.State.Color;
                positions[index] = DesktopSurfaceRenderModelAdapter.Convert(site.transform.localPosition);
                colors[index] = DesktopSurfaceRenderModelAdapter.Convert(color);
                sizes[index] = site.transform.localScale.x;
                visibility[index] = site.gameObject.activeSelf ? (byte)1 : (byte)0;
                flags[index] = Flags(site);
            }

            HBP.Core.Data.TemporalSample sample = source.CurrentProjectionSample;
            return new SiteRenderFrame(siteAssetHash, sourceStateRevision, new RenderTemporalSample(sample.Index, sample.Alpha), TemporalApplication.Linear, RenderBuffer<Float3>.TakeOwnership(positions), RenderBuffer<Rgba32>.TakeOwnership(colors), RenderBuffer<float>.TakeOwnership(sizes), RenderBuffer<byte>.TakeOwnership(visibility), RenderBuffer<SiteRenderFlags>.TakeOwnership(flags));
        }

        private static SiteRenderFlags Flags(Site site)
        {
            SiteRenderFlags result = SiteRenderFlags.None;
            if (site.IsSelected) result |= SiteRenderFlags.Selected;
            if (site.State.IsHighlighted) result |= SiteRenderFlags.Highlighted;
            if (site.State.IsBlackListed) result |= SiteRenderFlags.Blacklisted;
            if (site.State.IsMasked) result |= SiteRenderFlags.Masked;
            if (site.State.IsOutOfROI) result |= SiteRenderFlags.OutOfRoi;
            if (site.State.IsFiltered) result |= SiteRenderFlags.Filtered;
            return result;
        }
    }
}
