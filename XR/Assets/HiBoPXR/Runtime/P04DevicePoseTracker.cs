using UnityEngine;
using UnityEngine.InputSystem;

namespace CRNL.HiBoP.XR.Bootstrap
{
    public sealed class P04DevicePoseTracker : MonoBehaviour
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

            string positionControl = role == DeviceRole.Head ? "centerEyePosition" : "devicePosition";
            string rotationControl = role == DeviceRole.Head ? "centerEyeRotation" : "deviceRotation";

            positionAction = new InputAction($"P04 {role} position", binding: $"{layout}/{positionControl}");
            rotationAction = new InputAction($"P04 {role} rotation", binding: $"{layout}/{rotationControl}");
            trackedAction = new InputAction($"P04 {role} tracked", binding: $"{layout}/isTracked");

            positionAction.Enable();
            rotationAction.Enable();
            trackedAction.Enable();
        }

        private void OnDisable()
        {
            positionAction?.Dispose();
            rotationAction?.Dispose();
            trackedAction?.Dispose();
            positionAction = null;
            rotationAction = null;
            trackedAction = null;
        }

        private void Update()
        {
            IsTracked = trackedAction != null && trackedAction.ReadValue<float>() > 0.5f;

            if (diagnosticRenderer != null)
            {
                diagnosticRenderer.enabled = IsTracked;
            }

            if (!IsTracked || poseTarget == null || role == DeviceRole.Head)
            {
                return;
            }

            poseTarget.SetLocalPositionAndRotation(positionAction.ReadValue<Vector3>(), rotationAction.ReadValue<Quaternion>());
        }
    }
}
