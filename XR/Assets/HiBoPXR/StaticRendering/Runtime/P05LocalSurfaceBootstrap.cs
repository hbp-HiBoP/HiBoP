using System;
using CRNL.HiBoP.RenderModel;
using UnityEngine;

namespace CRNL.HiBoP.XR.StaticRendering
{
    public sealed class P05LocalSurfaceBootstrap : MonoBehaviour
    {
        [SerializeField] private P05StaticSurfaceRenderer anatomicalRenderer;
        [SerializeField] private P05StaticSurfaceRenderer inflatedRenderer;
        [SerializeField] private TextAsset anatomicalSurfaceData;
        [SerializeField] private TextAsset inflatedSurfaceData;

        public void Configure(P05StaticSurfaceRenderer anatomical, P05StaticSurfaceRenderer inflated, TextAsset anatomicalData, TextAsset inflatedData)
        {
            anatomicalRenderer = anatomical;
            inflatedRenderer = inflated;
            anatomicalSurfaceData = anatomicalData;
            inflatedSurfaceData = inflatedData;
        }

        private void Start()
        {
            if (anatomicalRenderer == null || inflatedRenderer == null || anatomicalSurfaceData == null || inflatedSurfaceData == null)
            {
                throw new InvalidOperationException("P05 local surface renderer and data references must be serialized in the prefab.");
            }

            SurfaceAsset anatomical = P05SurfaceAssetBinary.Read(anatomicalSurfaceData);
            SurfaceAsset inflated = P05SurfaceAssetBinary.Read(inflatedSurfaceData);
            if (anatomical.Representation != SurfaceRepresentation.Anatomical || inflated.Representation != SurfaceRepresentation.Inflated || anatomical.Hash == inflated.Hash)
            {
                throw new InvalidOperationException("P05-C requires distinct, explicitly represented anatomical and inflated SurfaceAssets.");
            }

            anatomicalRenderer.SetSurface(anatomical, SurfaceTransparency.Opaque);
            inflatedRenderer.SetSurface(inflated, SurfaceTransparency.Transparent);
            Debug.Log($"P05 local GIFTI-derived SurfaceAssets ready | anatomicalVertices={anatomical.Positions.Count} inflatedVertices={inflated.Positions.Count} indexFormat=UInt32");
        }
    }
}
