using UnityEngine;
using Unity.XR.CompositionLayers.Services;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.CompositionLayers;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.CompositionLayers;
using UnityEngine.XR.OpenXR.Features.Meta;

namespace HBP.Quest
{
    // The boundary controller owns transitions between passthrough and the opaque background.
    public sealed class QuestPassthroughStatus : MonoBehaviour
    {
        private static readonly Color SolidBackground = new(0.17f, 0.18f, 0.20f, 1f);

        [SerializeField] private ARCameraManager cameraManager;
        [SerializeField] private Camera xrCamera;
        private Color passthroughBackground;

        public bool IsSolidBackgroundActive { get; private set; }

        public void Configure(ARCameraManager manager, Camera camera)
        {
            cameraManager = manager;
            xrCamera = camera;
        }

        internal void SetSolidBackground(bool active)
        {
            if (active == IsSolidBackgroundActive) return;
            if (active)
            {
                passthroughBackground = xrCamera.backgroundColor;
                xrCamera.backgroundColor = SolidBackground;
                cameraManager.enabled = false;
            }
            else
            {
                if (cameraManager != null) cameraManager.enabled = true;
                if (xrCamera != null) xrCamera.backgroundColor = passthroughBackground;
            }

            IsSolidBackgroundActive = active;
            Debug.Log($"QUEST-004 environment={(active ? "Solid Background" : "Passthrough")}", this);
        }

        public string GetProblem()
        {
            var loader = XRGeneralSettings.Instance?.Manager?.activeLoader;
            if (loader is not OpenXRLoader) return "OpenXR loader unavailable";
            if (loader.GetLoadedSubsystem<XRDisplaySubsystem>()?.running != true) return "XR display stopped";
            if (xrCamera == null || !xrCamera.isActiveAndEnabled) return "XR render camera stopped";
            var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (pipeline == null || pipeline.supportsHDR || xrCamera.allowHDR || xrCamera.GetUniversalAdditionalCameraData().renderPostProcessing) return "Unexpected render configuration";
            if (IsSolidBackgroundActive)
                return xrCamera.clearFlags == CameraClearFlags.SolidColor && xrCamera.backgroundColor.a == 1 ? null : "Solid Background background is not opaque";
            if (!OpenXRRuntime.IsExtensionEnabled("XR_FB_passthrough")) return "XR_FB_passthrough unavailable";
            if (!OpenXRLayerProvider.isStarted) return "OpenXR composition provider stopped";
            if (cameraManager == null || !cameraManager.isActiveAndEnabled || cameraManager.subsystem?.running != true) return "Passthrough camera stopped";
            if (xrCamera.clearFlags != CameraClearFlags.SolidColor || xrCamera.backgroundColor.a != 0) return "Camera background is opaque";
            if (CompositionLayerManager.Instance != null)
            {
                foreach (var layer in CompositionLayerManager.Instance.CompositionLayers)
                    if (layer != null && layer.isActiveAndEnabled && layer.Order < 0 && layer.LayerData is PassthroughLayerData)
                        return null;
            }

            return "Passthrough composition layer missing";
        }
    }
}
