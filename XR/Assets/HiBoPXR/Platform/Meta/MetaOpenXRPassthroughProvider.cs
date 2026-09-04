using CRNL.HiBoP.XR.Bootstrap;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace CRNL.HiBoP.XR.Bootstrap.Meta
{
    public sealed class MetaOpenXRPassthroughProvider : MonoBehaviour, IPassthroughProvider
    {
        [SerializeField] private ARCameraManager cameraManager;

        [SerializeField] private Camera xrCamera;

        [SerializeField] private GameObject vrEnvironment;

        public bool IsAvailable => cameraManager != null && cameraManager.subsystem != null;

        public bool IsPassthroughActive => IsAvailable && cameraManager.enabled && cameraManager.subsystem.running;

        public void Configure(ARCameraManager manager, Camera camera, GameObject environment)
        {
            cameraManager = manager;
            xrCamera = camera;
            vrEnvironment = environment;
        }

        public bool TrySetPassthrough(bool enabled)
        {
            if (cameraManager == null || xrCamera == null)
            {
                return false;
            }

            xrCamera.clearFlags = CameraClearFlags.SolidColor;
            xrCamera.backgroundColor = enabled ? new Color(0f, 0f, 0f, 0f) : new Color(0.025f, 0.04f, 0.075f, 1f);
            cameraManager.enabled = enabled;

            if (vrEnvironment != null)
            {
                vrEnvironment.SetActive(!enabled);
            }

            return true;
        }
    }
}
