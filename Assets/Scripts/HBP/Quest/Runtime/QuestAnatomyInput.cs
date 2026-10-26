using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using HBP.Core.Tools;
using HBP.UI.Tools;
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
        [SerializeField] private QuestPointerInput pointer;
        [SerializeField] private QuestWindowsManager windows;
        private const float RecenterHoldSeconds = 2;
        private InputAction leftTrigger, rightTrigger, recenter;
        private CancellationTokenSource recenterStop;
        private bool recenterReleased;
        private bool focused = true;
        private bool paused;

        private void OnEnable()
        {
            leftTrigger = new InputAction("Grab left", InputActionType.Button, "<XRController>{LeftHand}/triggerPressed");
            rightTrigger = new InputAction("Grab right", InputActionType.Button, "<XRController>{RightHand}/triggerPressed");
            recenter = new InputAction("Hold to recenter (X)", InputActionType.Button, "<XRController>{LeftHand}/primaryButton");
            leftTrigger.Enable();
            rightTrigger.Enable();
            recenter.Enable();
        }

        private void LateUpdate()
        {
            UpdateRecenter();
            if (view == null || head == null || left == null || right == null) return;
            if (!focused || paused || !head.IsTracked)
            {
                CancelGrabs();
                return;
            }

            foreach (var presentation in view.Scenes)
                if (!presentation.IsPlaced)
                    Place(presentation);
            // A controller belongs to at most one column for the duration of a grab.
            var grabbed = System.Linq.Enumerable.FirstOrDefault(view.AllColumns, column => column.Manipulator.IsGrabbed);
            foreach (var column in view.AllColumns)
            {
                if (!view.CanInteractWithColumn(column))
                {
                    column?.Manipulator.CancelGrab();
                    continue;
                }

                if (grabbed != null && grabbed != column)
                {
                    column.Manipulator.CancelGrab();
                    continue;
                }

                Pose leftPose = ReadPose(left), rightPose = ReadPose(right);
                bool leftDistant = false, rightDistant = false;
                bool leftGrab = pointer != null && pointer.TryGetAnatomyPose(true, column.Manipulator, out leftPose, out leftDistant);
                bool rightGrab = pointer != null && pointer.TryGetAnatomyPose(false, column.Manipulator, out rightPose, out rightDistant);
                bool wasGrabbed = column.Manipulator.IsGrabbed;
                column.Manipulator.Step(leftPose, left.IsTracked, leftTrigger.IsPressed() && leftGrab, rightPose, right.IsTracked, rightTrigger.IsPressed() && rightGrab, leftDistant, rightDistant);
                if (!wasGrabbed && column.Manipulator.IsGrabbed) view.SelectContext(column.Scene, column.Column);
                if (column.Manipulator.IsGrabbed) grabbed = column;
            }
        }

        private bool CanRecenter => isActiveAndEnabled && focused && !paused && head != null && head.IsTracked && left != null && left.IsTracked;

        private void UpdateRecenter()
        {
            if (!CanRecenter)
            {
                CancelRecenter();
                return;
            }

            if (recenter.WasReleasedThisFrame()) recenterStop?.Cancel();
            if (!recenter.IsPressed())
            {
                recenterStop?.Cancel();
                recenterReleased = true;
            }
            else if (recenterReleased && recenter.WasPressedThisFrame() && recenterStop == null)
            {
                recenterReleased = false;
                recenterStop = new CancellationTokenSource();
                RecenterAfterHoldAsync(recenterStop).Forget();
            }
        }

        private async UniTask RecenterAfterHoldAsync(CancellationTokenSource stop)
        {
            try
            {
                await LoadingManager.PresentAsync(async (update, token) =>
                {
                    float started = Time.unscaledTime;
                    var text = new LoadingText(message: "Hold X to recenter");
                    update(0, 0, text);
                    while (true)
                    {
                        await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate, token, cancelImmediately: true);
                        if (!CanRecenter || !recenter.IsPressed()) stop.Cancel();
                        token.ThrowIfCancellationRequested();
                        float progress = (Time.unscaledTime - started) / RecenterHoldSeconds;
                        update(Mathf.Clamp01(progress), 0, text);
                        if (progress >= 1) break;
                    }

                    RecenterBrains();
                    if (windows != null) windows.RecenterOpen();
                }, stop.Token);
            }
            catch (OperationCanceledException)
            {
                // Releasing X or interrupting tracking leaves all placements untouched.
            }
            finally
            {
                recenterStop = null;
                stop.Dispose();
            }
        }

        private void CancelRecenter()
        {
            recenterReleased = false;
            recenterStop?.Cancel();
        }

        public void RecenterBrains()
        {
            if (view == null || head == null || !head.IsTracked) return;
            if (pointer != null) pointer.CancelCutGrabs();
            var placed = new System.Collections.Generic.List<Rect>();
            foreach (var presentation in view.Scenes)
                if (presentation.Scene.IsVisible && QuestAnatomySession.CanInteractWith(presentation.Scene))
                {
                    view.Place(presentation, ReadPose(head), Camera.main, placed);
                    if (view.TryProjectedBounds(presentation, QuestSceneLayout.Projection(ReadPose(head), Camera.main), out var bounds)) placed.Add(bounds);
                }
        }

        private void Place(QuestAnatomyView.Presentation presentation)
        {
            var occupied = new System.Collections.Generic.List<Rect>();
            var projection = QuestSceneLayout.Projection(ReadPose(head), Camera.main);
            foreach (var other in view.Scenes)
                if (other != presentation && other.IsPlaced && other.Scene.IsVisible && view.TryProjectedBounds(other, projection, out var bounds))
                    occupied.Add(bounds);
            view.Place(presentation, ReadPose(head), Camera.main, occupied);
        }

        private void CancelGrabs()
        {
            if (view != null)
                foreach (var column in view.AllColumns)
                    column.Manipulator.CancelGrab();
        }

        private static Pose ReadPose(Component tracker) => new Pose(tracker.transform.position, tracker.transform.rotation);

        private void OnApplicationFocus(bool value)
        {
            focused = value;
            if (!value) CancelRecenter();
            if (!value && view != null) CancelGrabs();
        }

        private void OnApplicationPause(bool value)
        {
            paused = value;
            if (value) CancelRecenter();
            if (value && view != null) CancelGrabs();
        }

        private void OnDisable()
        {
            CancelRecenter();
            leftTrigger?.Dispose();
            rightTrigger?.Dispose();
            recenter?.Dispose();
            leftTrigger = rightTrigger = recenter = null;
            if (view != null) CancelGrabs();
        }
    }
}
