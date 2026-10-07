using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace HBP.Quest
{
    public sealed class QuestPointerEventData : ExtendedPointerEventData
    {
        public Ray Ray;

        public QuestPointerEventData(EventSystem system) : base(system)
        {
            pointerType = UIPointerType.Tracked;
        }
    }

    /// <summary>Universal two-hand UI pointer and trigger arbiter. No pairing dependency.</summary>
    public sealed class QuestPointerInput : BaseInputModule
    {
        [SerializeField] private QuestInteractionPolicy policy;
        [SerializeField] private QuestDevicePoseTracker head, left, right;
        [SerializeField] private QuestAnatomyView anatomy;
        [SerializeField] private Transform trackingOrigin;
        [SerializeField] private LineRenderer leftRay, rightRay;
        [SerializeField] private Transform leftReticle, rightReticle;
        private readonly Hand[] hands = { new Hand(), new Hand() };
        private readonly List<RaycastResult> hits = new();
        private bool focused = true, paused;
        private MaterialPropertyBlock feedbackProperties;
#if DEVELOPMENT_BUILD && UNITY_ANDROID
        private string diagnosticOutput;
        private readonly object[] diagnosticHands = new object[2];
        private int diagnosticProcessCount;
        internal void RequestPointerDiagnostic(string output)
        {
            diagnosticOutput = output;
            System.Array.Clear(diagnosticHands, 0, diagnosticHands.Length);
        }
#endif

        private sealed class Hand
        {
            public readonly QuestInteractionCapture Capture = new();
            public QuestPointerEventData Data;
            public InputAction Trigger;
            public InputAction Position, Rotation;
            public QuestWindow Hovered;
            public Vector2 LastPosition;
            public QuestAnatomyManipulator AnatomyTarget;
            public Pose AnatomyPose;
            public bool Distant, AimContact;
            public float GrabDistance;
        }

        public bool AllowsAnatomy(bool leftHand) => hands[leftHand ? 0 : 1].Capture.Owner == QuestInteractionOwner.Anatomy;

        public bool TryGetAnatomyPose(bool leftHand, QuestAnatomyManipulator target, out Pose pose, out bool distant)
        {
            var hand = hands[leftHand ? 0 : 1];
            pose = hand.AnatomyPose;
            distant = hand.Distant;
            return AllowsAnatomy(leftHand) && hand.AnatomyTarget == target;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            feedbackProperties = new MaterialPropertyBlock();
            for (int i = 0; i < hands.Length; i++)
            {
                hands[i].Data = new QuestPointerEventData(eventSystem) { pointerId = -10 - i };
                hands[i].Trigger = new InputAction("Quest select", InputActionType.Button, i == 0 ? "<XRController>{LeftHand}/triggerPressed" : "<XRController>{RightHand}/triggerPressed");
                hands[i].Trigger.Enable();
                string device = i == 0 ? "<XRController>{LeftHand}" : "<XRController>{RightHand}";
                hands[i].Position = new InputAction("Quest aim position", binding: device + "/pointerPosition", expectedControlType: "Vector3");
                hands[i].Rotation = new InputAction("Quest aim rotation", binding: device + "/pointerRotation", expectedControlType: "Quaternion");
                hands[i].Position.Enable();
                hands[i].Rotation.Enable();
            }

            Feedback(leftRay, leftReticle, false, default, default);
            Feedback(rightRay, rightReticle, false, default, default);
        }

        public override void Process()
        {
            if (policy == null) return;
#if DEVELOPMENT_BUILD && UNITY_ANDROID
            diagnosticProcessCount++;
#endif
            // Presentation wrappers can move after the last physics tick.
            if (policy.EnableDistantAnatomy) Physics.SyncTransforms();
            ProcessHand(0, left, leftRay, leftReticle);
            ProcessHand(1, right, rightRay, rightReticle);
#if DEVELOPMENT_BUILD && UNITY_ANDROID
            if (diagnosticOutput != null)
            {
                string output = diagnosticOutput;
                diagnosticOutput = null;
                System.IO.File.WriteAllText(output, Newtonsoft.Json.JsonConvert.SerializeObject(new
                {
                    utc = System.DateTime.UtcNow, frame = Time.frameCount, processCount = diagnosticProcessCount,
                    build = Resources.Load<TextAsset>("BuildInfo")?.text, focused, paused, hands = diagnosticHands
                }, Newtonsoft.Json.Formatting.Indented));
            }
#endif
        }

        private void ProcessHand(int index, QuestDevicePoseTracker tracker, LineRenderer line, Transform reticle)
        {
            Hand hand = hands[index];
            var data = hand.Data;
            bool valid = focused && !paused && head != null && head.IsTracked && tracker != null && tracker.IsTracked;
            bool pressed = hand.Trigger.IsPressed();
            if (!valid)
            {
                Cancel(hand);
                Feedback(line, reticle, false, default, default);
                return;
            }

            var capturedWindow = data.pointerDrag != null ? data.pointerDrag.GetComponentInParent<QuestWindow>() : data.pointerPress != null ? data.pointerPress.GetComponentInParent<QuestWindow>() : null;
            if (hand.Capture.Owner == QuestInteractionOwner.UI && capturedWindow != null && !capturedWindow.IsOpen) Cancel(hand);
            if (hand.Capture.Owner == QuestInteractionOwner.Anatomy && !HasAnatomyTarget(hand.AnatomyTarget)) Cancel(hand);
            data.Ray = new Ray(tracker.transform.position, tracker.transform.forward);
            if (trackingOrigin != null && hand.Position.activeControl != null && hand.Rotation.activeControl != null)
                data.Ray = new Ray(trackingOrigin.TransformPoint(hand.Position.ReadValue<Vector3>()), trackingOrigin.rotation * hand.Rotation.ReadValue<Quaternion>() * Vector3.forward);
            data.trackedDevicePosition = data.Ray.origin;
            data.trackedDeviceOrientation = Quaternion.LookRotation(data.Ray.direction, tracker.transform.up);
            hits.Clear();
            eventSystem.RaycastAll(data, hits);
            data.pointerCurrentRaycast = default;
            foreach (var hit in hits)
                if (hit.isValid && hit.module is TrackedDeviceRaycaster && hit.distance <= policy.RayDistance)
                {
                    data.pointerCurrentRaycast = hit;
                    break;
                }

            GameObject target = data.pointerCurrentRaycast.gameObject;
            var window = target != null ? target.GetComponentInParent<QuestWindow>() : null;
            if (window != hand.Hovered)
            {
                if (hand.Hovered != null) hand.Hovered.SetHovered(index, false);
                hand.Hovered = window;
                if (window != null) window.SetHovered(index, true);
            }

            HandlePointerExitAndEnter(data, target);
            data.position = data.pointerCurrentRaycast.screenPosition;
            data.delta = data.position - hand.LastPosition;
            hand.LastPosition = data.position;
            var candidate = AnatomyCandidate(tracker.transform.position, target == null, data.Ray.origin);
            bool near = candidate != null;
            float distantDistance = policy.RayDistance;
            var distantCandidate = !near && policy.EnableDistantAnatomy ? DistantAnatomyCandidate(data.Ray, out distantDistance) : null;
            bool uiFirst = target != null && (near || distantCandidate == null || data.pointerCurrentRaycast.distance <= distantDistance);
            var oldOwner = hand.Capture.Owner;
            bool began = hand.Capture.Sample(true, pressed, uiFirst, near || distantCandidate != null, near ? policy.PreferNearbyAnatomy : !uiFirst);
            if (began && hand.Capture.Owner == QuestInteractionOwner.Anatomy)
            {
                hand.AnatomyTarget = near ? candidate : distantCandidate;
                hand.Distant = !near;
                // Quest grip and aim origins differ; keep the point that started this contact for the entire gesture.
                hand.AimContact = near && !candidate.CanGrab(tracker.transform.position) && candidate.CanGrab(data.Ray.origin);
                hand.GrabDistance = distantDistance;
            }

            hand.AnatomyPose = hand.Distant ? new Pose(data.Ray.GetPoint(hand.GrabDistance), data.trackedDeviceOrientation) : new Pose(hand.AimContact ? data.Ray.origin : tracker.transform.position, tracker.transform.rotation);
            if (!pressed)
            {
                hand.AnatomyTarget = null;
                hand.AimContact = false;
            }

            if (began && hand.Capture.Owner == QuestInteractionOwner.UI)
            {
                data.pressPosition = data.position;
                data.pointerPressRaycast = data.pointerCurrentRaycast;
                data.eligibleForClick = true;
                data.pointerPress = ExecuteEvents.ExecuteHierarchy(target, data, ExecuteEvents.pointerDownHandler);
                if (data.pointerPress == null) data.pointerPress = ExecuteEvents.GetEventHandler<IPointerClickHandler>(target);
                data.rawPointerPress = target;
                data.pointerDrag = ExecuteEvents.GetEventHandler<IDragHandler>(target);
                if (data.pointerDrag != null) ExecuteEvents.Execute(data.pointerDrag, data, ExecuteEvents.initializePotentialDrag);
            }

            if (oldOwner == QuestInteractionOwner.UI && !pressed) Release(hand, target, true);
            else if (hand.Capture.Owner == QuestInteractionOwner.UI)
            {
                if (data.pointerPress == null && data.pointerDrag == null) hand.Capture.Cancel();
                else if (data.pointerDrag != null)
                {
                    // Controller rotation can keep the hit in place while the ray origin moves.
                    if (!data.dragging && (!data.useDragThreshold || (data.position - data.pressPosition).sqrMagnitude >= eventSystem.pixelDragThreshold * eventSystem.pixelDragThreshold))
                    {
                        ExecuteEvents.Execute(data.pointerDrag, data, ExecuteEvents.beginDragHandler);
                        data.dragging = true;
                        data.eligibleForClick = false;
                    }

                    if (data.dragging) ExecuteEvents.Execute(data.pointerDrag, data, ExecuteEvents.dragHandler);
                }
            }

            bool actionable = target != null && (window != null || ExecuteEvents.GetEventHandler<IPointerClickHandler>(target) != null || ExecuteEvents.GetEventHandler<IDragHandler>(target) != null);
            bool distantFeedback = hand.Capture.Owner == QuestInteractionOwner.Anatomy && hand.Distant || hand.Capture.Owner == QuestInteractionOwner.None && !near && distantCandidate != null && !uiFirst;
            // Direct grabbing needs no ray; an existing UI capture keeps its feedback until release.
            bool useful = hand.Capture.Owner == QuestInteractionOwner.UI || (!near && (distantFeedback || (hand.Capture.Owner == QuestInteractionOwner.None && actionable && uiFirst)));
            Vector3 end = data.pointerCurrentRaycast.isValid ? data.pointerCurrentRaycast.worldPosition : data.Ray.GetPoint(policy.RayDistance);
            if (distantFeedback) end = data.Ray.GetPoint(hand.Capture.Owner == QuestInteractionOwner.Anatomy ? hand.GrabDistance : distantDistance);
            Feedback(line, reticle, useful, data.Ray.origin, end, !distantFeedback);
#if DEVELOPMENT_BUILD && UNITY_ANDROID
            if (diagnosticOutput != null)
            {
                var brains = new List<object>();
                if (anatomy != null)
                    foreach (var column in anatomy.Columns)
                    {
                        if (column == null || column.Column == null || column.Manipulator.SharedMesh == null) continue;
                        var mesh = column.Manipulator.SharedMesh;
                        var frame = column.Column.BrainMesh.transform;
                        var gripLocal = frame.InverseTransformPoint(tracker.transform.position);
                        var aimLocal = frame.InverseTransformPoint(data.Ray.origin);
                        brains.Add(new
                        {
                            name = column.Column.Name, center = DiagnosticVector(mesh.bounds.center), size = DiagnosticVector(mesh.bounds.size),
                            meshWorldPosition = DiagnosticVector(frame.position), meshWorldScale = DiagnosticVector(frame.lossyScale),
                            gripLocal = DiagnosticVector(gripLocal), aimLocal = DiagnosticVector(aimLocal),
                            gripInside = mesh.bounds.Contains(gripLocal), aimInside = mesh.bounds.Contains(aimLocal),
                            gripDistanceMeters = Vector3.Distance(tracker.transform.position, column.Manipulator.ClosestGrabPoint(tracker.transform.position)),
                            grabbed = column.Manipulator.IsGrabbed
                        });
                    }
                diagnosticHands[index] = new
                {
                    hand = index == 0 ? "left" : "right", pressed, near, candidate = candidate?.name,
                    distantCandidate = distantCandidate?.name, distantDistance, uiTarget = target?.name, uiFirst,
                    owner = hand.Capture.Owner.ToString(), capturedTarget = hand.AnatomyTarget?.name, hand.Distant, hand.AimContact, hand.GrabDistance,
                    grip = DiagnosticVector(tracker.transform.position), aim = DiagnosticVector(data.Ray.origin),
                    aimControl = hand.Position.activeControl?.path, gripControl = tracker.DiagnosticPositionControl,
                    rawGrip = DiagnosticVector(tracker.DiagnosticPositionValue), trackerFrame = tracker.DiagnosticPoseFrame,
                    useful, lineEnabled = line != null && line.enabled, lineEnd = DiagnosticVector(end), brains
                };
            }
#endif
        }

#if DEVELOPMENT_BUILD && UNITY_ANDROID
        private static object DiagnosticVector(Vector3 value) => new { value.x, value.y, value.z };
#endif

        private bool HasAnatomyTarget(QuestAnatomyManipulator target)
        {
            if (target == null || anatomy == null || target.SharedMesh == null) return false;
            foreach (var column in anatomy.Columns)
                if (column != null && column.Manipulator == target)
                    return true;
            return false;
        }

        private QuestAnatomyManipulator DistantAnatomyCandidate(Ray ray, out float distance)
        {
            distance = policy.RayDistance;
            QuestAnatomyManipulator nearest = null;
            if (anatomy == null || anatomy.SurfaceHidden) return null;
            QuestAnatomyManipulator grabbed = null;
            foreach (var column in anatomy.Columns)
                if (column != null && column.Manipulator.IsGrabbed)
                {
                    grabbed = column.Manipulator;
                    break;
                }

            foreach (var column in anatomy.Columns)
                if (column != null && (grabbed == null || grabbed == column.Manipulator) && column.Manipulator.Raycast(ray, distance, out float hitDistance))
                {
                    nearest = column.Manipulator;
                    distance = hitDistance;
                }

            return nearest;
        }

        private QuestAnatomyManipulator AnatomyCandidate(Vector3 position, bool allowJoining, Vector3? aimPosition = null)
        {
            if (anatomy == null) return null;
            foreach (var column in anatomy.Columns)
                if (column != null && column.Manipulator.IsGrabbed)
                {
                    if (column.Manipulator.CanGrab(position) || (aimPosition.HasValue && column.Manipulator.CanGrab(aimPosition.Value))) return column.Manipulator;
                    // Preserve joining a direct gesture, but don't mix physical and remote poses accidentally.
                    if (allowJoining)
                        foreach (var hand in hands)
                            if (hand.Capture.Owner == QuestInteractionOwner.Anatomy && hand.AnatomyTarget == column.Manipulator && !hand.Distant)
                                return column.Manipulator;
                    return null;
                }

            QuestAnatomyManipulator nearest = null;
            float nearestCenter = float.PositiveInfinity;
            foreach (var column in anatomy.Columns)
                if (column != null && (column.Manipulator.CanGrab(position) || (aimPosition.HasValue && column.Manipulator.CanGrab(aimPosition.Value))))
                {
                    float distance = (position - column.Manipulator.GrabCenter).sqrMagnitude;
                    if (distance < nearestCenter)
                    {
                        nearest = column.Manipulator;
                        nearestCenter = distance;
                    }
                }

            return nearest;
        }

        private void Feedback(LineRenderer line, Transform reticle, bool visible, Vector3 start, Vector3 end, bool showReticle = true)
        {
            if (line != null)
            {
                line.enabled = visible;
                line.positionCount = 2;
                line.useWorldSpace = true;
                line.startWidth = line.endWidth = policy != null ? policy.RayWidth : 0.002f;
                if (policy != null && policy.FeedbackColor != null) line.startColor = line.endColor = policy.FeedbackColor.Value;
                ApplyFeedbackColor(line);
                if (visible)
                {
                    line.SetPosition(0, start);
                    line.SetPosition(1, end);
                }
            }

            if (reticle != null)
            {
                reticle.gameObject.SetActive(visible && showReticle);
                ApplyFeedbackColor(reticle.GetComponent<Renderer>());
                if (visible) reticle.position = end;
            }
        }

        private void ApplyFeedbackColor(Renderer renderer)
        {
            if (renderer == null || policy == null || policy.FeedbackColor == null) return;
            feedbackProperties.SetColor("_BaseColor", policy.FeedbackColor.Value);
            feedbackProperties.SetColor("_Color", policy.FeedbackColor.Value);
            renderer.SetPropertyBlock(feedbackProperties);
        }

        private void Release(Hand hand, GameObject target, bool click)
        {
            var data = hand.Data;
            if (data.pointerPress != null) ExecuteEvents.Execute(data.pointerPress, data, ExecuteEvents.pointerUpHandler);
            if (click && data.eligibleForClick && data.pointerPress == ExecuteEvents.GetEventHandler<IPointerClickHandler>(target))
                ExecuteEvents.Execute(data.pointerPress, data, ExecuteEvents.pointerClickHandler);
            if (data.dragging && data.pointerDrag != null) ExecuteEvents.Execute(data.pointerDrag, data, ExecuteEvents.endDragHandler);
            data.eligibleForClick = data.dragging = false;
            data.pointerPress = data.rawPointerPress = data.pointerDrag = null;
        }

        private void Cancel(Hand hand)
        {
            if (hand.Data != null)
            {
                Release(hand, null, false);
                HandlePointerExitAndEnter(hand.Data, null);
            }

            if (hand.Hovered != null) hand.Hovered.SetHovered(hand == hands[0] ? 0 : 1, false);
            hand.Hovered = null;
            hand.Capture.Cancel();
            hand.AnatomyTarget = null;
            hand.Distant = false;
            hand.AimContact = false;
        }

        private void OnApplicationFocus(bool value)
        {
            focused = value;
            if (!value)
                foreach (var hand in hands)
                    Cancel(hand);
        }

        private void OnApplicationPause(bool value)
        {
            paused = value;
            if (value)
                foreach (var hand in hands)
                    Cancel(hand);
        }

        protected override void OnDisable()
        {
            foreach (var hand in hands)
            {
                Cancel(hand);
                hand.Trigger?.Dispose();
                hand.Position?.Dispose();
                hand.Rotation?.Dispose();
            }

            Feedback(leftRay, leftReticle, false, default, default);
            Feedback(rightRay, rightReticle, false, default, default);
            base.OnDisable();
        }
    }
}
