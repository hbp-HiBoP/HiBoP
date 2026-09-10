using UnityEngine;
using UnityEngine.InputSystem;

namespace HBP.Quest.Legacy
{
    /// <summary>Controller adapter; contains no Desktop or network command path.</summary>
    public sealed class QuestAnatomyInput : MonoBehaviour
    {
        [SerializeField] private QuestAnatomyView view;
        [SerializeField] private QuestAnatomyManipulator manipulator;
        [SerializeField] private QuestDevicePoseTracker head;
        [SerializeField] private QuestDevicePoseTracker left;
        [SerializeField] private QuestDevicePoseTracker right;
        private InputAction leftTrigger, rightTrigger, recenter, toggleSurface, recalculate;
        private Mesh previousMesh;
        private bool placed;
        private bool focused = true;
        private bool paused;

        private void OnEnable()
        {
            leftTrigger = new InputAction("Grab left", InputActionType.Button, "<XRController>{LeftHand}/triggerPressed");
            rightTrigger = new InputAction("Grab right", InputActionType.Button, "<XRController>{RightHand}/triggerPressed");
            recenter = new InputAction("Recenter anatomy (X)", InputActionType.Button, "<XRController>{LeftHand}/primaryButton");
            toggleSurface = new InputAction("Hide/show brain (A)", InputActionType.Button, "<XRController>{RightHand}/primaryButton");
            recalculate = new InputAction("Recalculate density", InputActionType.Button, "<XRController>{RightHand}/thumbstickClicked");
            recalculate.Enable();
            leftTrigger.Enable();
            rightTrigger.Enable();
            recenter.Enable();
            toggleSurface.Enable();
        }

        private void LateUpdate()
        {
            if (view == null || manipulator == null || head == null || left == null || right == null) return;
            if (!focused || paused || !head.IsTracked)
            {
                manipulator.CancelGrab();
                return;
            }

            if (previousMesh != view.SharedMesh)
            {
                manipulator.CancelGrab();
                previousMesh = view.SharedMesh;
            }

            if (right.IsTracked && recalculate.WasPressedThisFrame()) view.RecalculateProjection();
            if (right.IsTracked && toggleSurface.WasPressedThisFrame()) view.ToggleSurface();

            if (view.SharedMesh != null && (!placed || (left.IsTracked && recenter.WasPressedThisFrame())))
            {
                manipulator.Recenter(ReadPose(head));
                placed = true;
                return;
            }

            manipulator.Step(ReadPose(left), left.IsTracked, leftTrigger.IsPressed(), ReadPose(right), right.IsTracked, rightTrigger.IsPressed());
        }

        private static Pose ReadPose(Component tracker) => new Pose(tracker.transform.position, tracker.transform.rotation);

        private void OnApplicationFocus(bool value)
        {
            focused = value;
            if (!value && manipulator != null) manipulator.CancelGrab();
        }

        private void OnApplicationPause(bool value)
        {
            paused = value;
            if (value && manipulator != null) manipulator.CancelGrab();
        }

        private void OnDisable()
        {
            leftTrigger?.Dispose();
            rightTrigger?.Dispose();
            recenter?.Dispose();
            toggleSurface?.Dispose();
            recalculate?.Dispose();
            leftTrigger = rightTrigger = recenter = toggleSurface = null;
            if (manipulator != null) manipulator.CancelGrab();
        }
    }
}
