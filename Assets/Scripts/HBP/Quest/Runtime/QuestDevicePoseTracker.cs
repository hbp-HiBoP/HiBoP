using System;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.OpenXR;

namespace HBP.Quest
{
    internal enum QuestControllerControl
    {
        PrimaryButton,
        SecondaryButton,
        Thumbstick,
        FrontTrigger,
        SideGrip
    }

    // Adapted from P04DevicePoseTracker on feature/xr. Head movement is owned by TrackedPoseDriver.
    public sealed class QuestDevicePoseTracker : MonoBehaviour
    {
        public enum DeviceRole
        {
            Head,
            LeftController,
            RightController
        }

        [SerializeField] private DeviceRole role;
        [SerializeField] private Transform poseTarget;
        [SerializeField] private Renderer diagnosticRenderer;
        [SerializeField] private MeshRenderer solidBackgroundController;
        [SerializeField] private MeshFilter controllerHelpMesh;

        // Surface points on the named Touch Plus parts in the neutral WebXR model, in Unity mesh space.
        [SerializeField] private Vector3 primaryButton, secondaryButton, thumbstick, frontTrigger, sideGrip;
        private QuestPassthroughStatus passthrough;
        private MeshRenderer controllerHelpRenderer;
        private MeshFilter solidMesh;
        private QuestControllerModelGeometry nativeController;
        private int modelDeviceId;
        private bool modelReadFailed;
        private float nextModelRequest;
        private bool controllerHelpVisible;
        private InputAction positionAction;
        private InputAction rotationAction;
        private InputAction trackedAction;
        private InputAction trackingStateAction;

        public bool IsTracked { get; private set; }
        public DeviceRole Role => role;
        internal bool HasControllerHelp => controllerHelpMesh != null;
#if DEVELOPMENT_BUILD && UNITY_ANDROID
        internal string DiagnosticPositionControl => positionAction?.activeControl?.path;
        internal Vector3 DiagnosticPositionValue => positionAction != null ? positionAction.ReadValue<Vector3>() : default;
        internal int DiagnosticPoseFrame { get; private set; }
#endif

        public void Configure(DeviceRole configuredRole, Transform target, Renderer marker)
        {
            role = configuredRole;
            poseTarget = target;
            diagnosticRenderer = marker;
        }

        private void OnEnable()
        {
            string layout = role switch
            {
                DeviceRole.Head => "<XRHMD>",
                DeviceRole.LeftController => "<XRController>{LeftHand}",
                _ => "<XRController>{RightHand}"
            };
            positionAction = new InputAction("Quest position", binding: $"{layout}/{(role == DeviceRole.Head ? "centerEyePosition" : "devicePosition")}");
            rotationAction = new InputAction("Quest rotation", binding: $"{layout}/{(role == DeviceRole.Head ? "centerEyeRotation" : "deviceRotation")}");
            trackedAction = new InputAction("Quest tracked", binding: $"{layout}/isTracked");
            trackingStateAction = new InputAction("Quest tracking state", binding: $"{layout}/trackingState");
            positionAction.Enable();
            rotationAction.Enable();
            trackedAction.Enable();
            trackingStateAction.Enable();
            if (diagnosticRenderer != null) diagnosticRenderer.enabled = false;
            if (solidBackgroundController != null)
            {
                passthrough = GetComponentInParent<QuestPassthroughStatus>();
                solidMesh = solidBackgroundController.GetComponent<MeshFilter>();
                if (controllerHelpMesh != null)
                {
                    controllerHelpRenderer = controllerHelpMesh.GetComponent<MeshRenderer>();
                    UseFallbackController();
                }
            }

            Application.onBeforeRender += UpdatePose;
        }

        private void OnDisable()
        {
            Application.onBeforeRender -= UpdatePose;
            positionAction?.Dispose();
            rotationAction?.Dispose();
            trackedAction?.Dispose();
            trackingStateAction?.Dispose();
            positionAction = rotationAction = trackedAction = trackingStateAction = null;
            IsTracked = false;
            controllerHelpVisible = false;
            if (diagnosticRenderer != null) diagnosticRenderer.enabled = false;
            UpdateControllerAppearance();
            if (controllerHelpMesh != null) UseFallbackController();
            modelDeviceId = 0;
            modelReadFailed = false;
            nextModelRequest = 0;
        }

        internal void SetControllerHelpVisible(bool visible)
        {
            if (controllerHelpVisible == visible) return;
            controllerHelpVisible = visible;
            UpdateControllerAppearance();
        }

        internal Vector3 ControllerHelpAnchor(QuestControllerControl control)
        {
            Vector3 point = nativeController != null ? nativeController.Anchor(control) : control switch
            {
                QuestControllerControl.PrimaryButton => primaryButton,
                QuestControllerControl.SecondaryButton => secondaryButton,
                QuestControllerControl.Thumbstick => thumbstick,
                QuestControllerControl.FrontTrigger => frontTrigger,
                _ => sideGrip
            };
            return controllerHelpMesh.transform.TransformPoint(point);
        }

        internal bool HasControllerHelpAnchor(QuestControllerControl control) => nativeController == null || nativeController.HasAnchor(control);

        private void Update()
        {
            UpdatePose();
            if (IsTracked && HasControllerHelp) UpdateControllerModel();
        }

        private void UpdateControllerModel()
        {
            int deviceId = positionAction.activeControl?.device.deviceId ?? 0;
            if (deviceId == 0) return;
            if (modelDeviceId != deviceId)
            {
                modelDeviceId = deviceId;
                modelReadFailed = false;
                nextModelRequest = 0;
                UseFallbackController();
            }

            float now = Time.unscaledTime;
            if (nativeController != null || modelReadFailed || now < nextModelRequest) return;
            nextModelRequest = now + 2;
            var runtime = OpenXRSettings.Instance?.GetFeature<QuestControllerRenderModels>();
            bool left = role == DeviceRole.LeftController;
            if (runtime == null || !runtime.TryLoad(left, out var bytes, out string identity)) return;

            try
            {
                nativeController = QuestControllerModelGeometry.Read(bytes, left);
                controllerHelpMesh.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                controllerHelpMesh.transform.localScale = Vector3.one;
                controllerHelpMesh.sharedMesh = nativeController.Mesh;
                UpdateControllerAppearance();
                Debug.Log($"[Quest controller models] Using native contour for {role}: {identity}, {nativeController.Mesh.vertexCount} vertices.");
            }
            catch (Exception error) when (error is FormatException or IOException or Newtonsoft.Json.JsonException or ArgumentException or InvalidOperationException)
            {
                modelReadFailed = true;
                Debug.LogWarning($"[Quest controller models] Using the HiBoP model for {role}: {error.Message}");
            }
        }

        private void UseFallbackController()
        {
            controllerHelpMesh.sharedMesh = solidMesh != null ? solidMesh.sharedMesh : null;
            if (solidMesh != null)
            {
                var fallbackPose = solidMesh.transform;
                controllerHelpMesh.transform.SetLocalPositionAndRotation(fallbackPose.localPosition, fallbackPose.localRotation);
                controllerHelpMesh.transform.localScale = fallbackPose.localScale;
            }

            if (nativeController == null) return;
            Destroy(nativeController.Mesh);
            nativeController = null;
        }

        private void UpdatePose()
        {
#if DEVELOPMENT_BUILD && UNITY_ANDROID
            DiagnosticPoseFrame = Time.frameCount;
#endif
            // Require both position and rotation: a stale or partially tracked marker is misleading.
            IsTracked = trackedAction != null && trackedAction.ReadValue<float>() > 0.5f && (trackingStateAction.ReadValue<int>() & 3) == 3;
            if (diagnosticRenderer != null) diagnosticRenderer.enabled = IsTracked;
            if (IsTracked && poseTarget != null && role != DeviceRole.Head)
                poseTarget.SetLocalPositionAndRotation(positionAction.ReadValue<Vector3>(), rotationAction.ReadValue<Quaternion>());
            UpdateControllerAppearance();
        }

        private void UpdateControllerAppearance()
        {
            if (solidBackgroundController == null) return;
            bool contour = IsTracked && controllerHelpVisible;
            // The native mesh and the visual fallback have different registration.
            bool solidBackground = passthrough != null && passthrough.IsSolidBackgroundActive;
            solidBackgroundController.enabled = IsTracked && solidBackground && !(contour && nativeController != null);
            if (controllerHelpRenderer != null)
                controllerHelpRenderer.enabled = contour;
        }
    }
}
