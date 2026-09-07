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
    // P04's camera/subsystem checks, without its silent opaque VR fallback.
    public sealed class QuestPassthroughStatus : MonoBehaviour
    {
        [SerializeField] private ARCameraManager cameraManager;
        [SerializeField] private Camera xrCamera;

        public void Configure(ARCameraManager manager, Camera camera)
        {
            cameraManager = manager;
            xrCamera = camera;
        }

        public string GetProblem()
        {
            var loader = XRGeneralSettings.Instance?.Manager?.activeLoader;
            if (loader is not OpenXRLoader) return "OpenXR loader unavailable";
            if (loader.GetLoadedSubsystem<XRDisplaySubsystem>()?.running != true) return "XR display stopped";
            if (!OpenXRRuntime.IsExtensionEnabled("XR_FB_passthrough")) return "XR_FB_passthrough unavailable";
            if (!OpenXRLayerProvider.isStarted) return "OpenXR composition provider stopped";
            if (cameraManager == null || !cameraManager.isActiveAndEnabled || cameraManager.subsystem?.running != true) return "Passthrough camera stopped";
            if (xrCamera == null || xrCamera.clearFlags != CameraClearFlags.SolidColor || xrCamera.backgroundColor.a != 0) return "Camera background is opaque";
            var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (pipeline == null || pipeline.supportsHDR || xrCamera.allowHDR || xrCamera.GetUniversalAdditionalCameraData().renderPostProcessing) return "Unexpected render configuration";
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
