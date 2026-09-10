using UnityEngine;
using UnityEngine.InputSystem;

namespace HBP.Quest
{
    /// <summary>Controller adapter; contains no Desktop or network command path.</summary>
    public sealed class QuestAnatomyInput : MonoBehaviour
    {
        [SerializeField] private QuestAnatomyView view;
        [SerializeField] private QuestDevicePoseTracker head;
        [SerializeField] private QuestDevicePoseTracker left;
        [SerializeField] private QuestDevicePoseTracker right;
        private InputAction leftTrigger, rightTrigger, recenter, toggleSurface, recalculate;
        private HBP.Data.Module3D.Base3DScene previousScene;
        private bool placed;
        private bool focused = true;
        private bool paused;

        private void OnEnable()
        {
            leftTrigger = new InputAction("Grab left", InputActionType.Button, "<XRController>{LeftHand}/triggerPressed");
            rightTrigger = new InputAction("Grab right", InputActionType.Button, "<XRController>{RightHand}/triggerPressed");
            recenter = new InputAction("Recenter columns (X)", InputActionType.Button, "<XRController>{LeftHand}/primaryButton");
            toggleSurface = new InputAction("Hide/show brain (A)", InputActionType.Button, "<XRController>{RightHand}/primaryButton");
            recalculate = new InputAction("Recalculate projection", InputActionType.Button, "<XRController>{RightHand}/thumbstickClicked");
            recalculate.Enable();
            leftTrigger.Enable();
            rightTrigger.Enable();
            recenter.Enable();
            toggleSurface.Enable();
        }

        private void LateUpdate()
        {
            if (view == null || head == null || left == null || right == null) return;
            if (!focused || paused || !head.IsTracked)
            {
                CancelGrabs();
                return;
            }

            if (previousScene != view.Scene)
            {
                CancelGrabs();
                previousScene = view.Scene;
                placed = false;
            }

            if (right.IsTracked && recalculate.WasPressedThisFrame()) view.RecalculateProjection();
            if (right.IsTracked && toggleSurface.WasPressedThisFrame()) view.ToggleSurface();

            if (view.Scene != null && (!placed || (left.IsTracked && recenter.WasPressedThisFrame())))
            {
                int index = 0;
                foreach (var column in view.Columns)
                {
                    column.Manipulator.Recenter(ReadPose(head));
                    column.transform.position += head.transform.right * (index++ - (view.Columns.Count - 1) * 0.5f) * 0.35f;
                }

                placed = true;
                return;
            }

            // A controller belongs to at most one column for the duration of a grab.
            var grabbed = System.Linq.Enumerable.FirstOrDefault(view.Columns, column => column.Manipulator.IsGrabbed);
            foreach (var column in view.Columns)
            {
                if (grabbed != null && grabbed != column)
                {
                    column.Manipulator.CancelGrab();
                    continue;
                }

                column.Manipulator.Step(ReadPose(left), left.IsTracked, leftTrigger.IsPressed(), ReadPose(right), right.IsTracked, rightTrigger.IsPressed());
                if (column.Manipulator.IsGrabbed) grabbed = column;
            }
        }

        private void CancelGrabs()
        {
            if (view != null)
                foreach (var column in view.Columns)
                    column.Manipulator.CancelGrab();
        }

        private static Pose ReadPose(Component tracker) => new Pose(tracker.transform.position, tracker.transform.rotation);

        private void OnApplicationFocus(bool value)
        {
            focused = value;
            if (!value && view != null) CancelGrabs();
        }

        private void OnApplicationPause(bool value)
        {
            paused = value;
            if (value && view != null) CancelGrabs();
        }

        private void OnDisable()
        {
            leftTrigger?.Dispose();
            rightTrigger?.Dispose();
            recenter?.Dispose();
            toggleSurface?.Dispose();
            recalculate?.Dispose();
            leftTrigger = rightTrigger = recenter = toggleSurface = null;
            if (view != null) CancelGrabs();
        }
    }
}
