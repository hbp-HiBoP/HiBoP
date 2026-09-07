using UnityEngine;
using UnityEngine.InputSystem;

namespace HBP.Quest
{
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
        private InputAction positionAction;
        private InputAction rotationAction;
        private InputAction trackedAction;
        private InputAction trackingStateAction;

        public bool IsTracked { get; private set; }
        public DeviceRole Role => role;

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
            if (diagnosticRenderer != null) diagnosticRenderer.enabled = false;
        }

        private void Update() => UpdatePose();

        private void UpdatePose()
        {
            // Require both position and rotation: a stale or partially tracked marker is misleading.
            IsTracked = trackedAction != null && trackedAction.ReadValue<float>() > 0.5f && (trackingStateAction.ReadValue<int>() & 3) == 3;
            if (diagnosticRenderer != null) diagnosticRenderer.enabled = IsTracked;
            if (IsTracked && poseTarget != null && role != DeviceRole.Head)
                poseTarget.SetLocalPositionAndRotation(positionAction.ReadValue<Vector3>(), rotationAction.ReadValue<Quaternion>());
        }
    }
}
