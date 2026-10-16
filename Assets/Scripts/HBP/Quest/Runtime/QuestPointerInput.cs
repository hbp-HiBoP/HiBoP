using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

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
        [SerializeField] private QuestSiteProbe siteProbe;
        [SerializeField] private QuestCutHandles cuts;
        [SerializeField] private Transform trackingOrigin;
        [SerializeField] private LineRenderer leftRay, rightRay;
        [SerializeField] private Transform leftReticle, rightReticle;
        [SerializeField] private Material overlayFeedbackMaterial;
        private readonly Hand[] hands = { new Hand(), new Hand() };
        private readonly List<RaycastResult> hits = new();
        private bool focused = true, paused;
        private QuestDialogBox modalDialog;
        private MaterialPropertyBlock feedbackProperties;
        private readonly Dictionary<Renderer, (Material Material, int SortingOrder)> feedbackDefaults = new();
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
        public bool IsAnatomyCaptured(QuestAnatomyManipulator target) => System.Array.Exists(hands, hand => hand.Capture.Owner == QuestInteractionOwner.Anatomy && hand.AnatomyTarget == target);

        public void CancelCutGrabs()
        {
            foreach (var hand in hands)
                if (hand.Capture.Owner == QuestInteractionOwner.Cut)
                    Cancel(hand);
            if (cuts != null) cuts.CancelAll();
        }

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
            feedbackDefaults.Clear();
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
            RefreshModal();
#if DEVELOPMENT_BUILD && UNITY_ANDROID
            diagnosticProcessCount++;
#endif
            // Presentation wrappers can move after the last physics tick.
            if (policy.EnableDistantAnatomy) Physics.SyncTransforms();
            if (cuts != null) cuts.Refresh();
            ProcessHand(0, left, leftRay, leftReticle);
            RefreshModal(); // The first hand's click may open or close a dialog in this same frame.
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

        private void RefreshModal()
        {
            var dialog = QuestDialogBox.Current;
            if (ReferenceEquals(modalDialog, dialog)) return;
            foreach (var hand in hands) Cancel(hand);
            if (siteProbe != null) siteProbe.Cancel();
            modalDialog = dialog;
        }

        private void ProcessHand(int index, QuestDevicePoseTracker tracker, LineRenderer line, Transform reticle)
        {
            Hand hand = hands[index];
            var data = hand.Data;
            bool valid = focused && !paused && head != null && head.IsTracked && tracker != null && tracker.IsTracked;
            bool pressed = hand.Trigger.IsPressed();
            if (valid)
            {
                data.Ray = new Ray(tracker.transform.position, tracker.transform.forward);
                if (trackingOrigin != null && hand.Position.activeControl != null && hand.Rotation.activeControl != null)
                    data.Ray = new Ray(trackingOrigin.TransformPoint(hand.Position.ReadValue<Vector3>()), trackingOrigin.rotation * hand.Rotation.ReadValue<Quaternion>() * Vector3.forward);
            }

            if (cuts != null) cuts.SetHand(index, valid, tracker != null ? tracker.transform.position : Vector3.zero, data.Ray.origin);

            if (modalDialog == null && index == 1 && siteProbe != null && siteProbe.Process(valid, hand.Capture.Owner != QuestInteractionOwner.None, data.Ray))
            {
                Cancel(hand);
                Feedback(line, reticle, false, default, default);
                return;
            }

            if (!valid)
            {
                Cancel(hand);
                Feedback(line, reticle, false, default, default);
                return;
            }

            var capturedWindow = data.pointerDrag != null ? data.pointerDrag.GetComponentInParent<QuestWindow>() : data.pointerPress != null ? data.pointerPress.GetComponentInParent<QuestWindow>() : null;
            if (hand.Capture.Owner == QuestInteractionOwner.UI && capturedWindow != null && !capturedWindow.IsOpen) Cancel(hand);
            if (hand.Capture.Owner == QuestInteractionOwner.Anatomy && !HasAnatomyTarget(hand.AnatomyTarget)) Cancel(hand);
            if (hand.Capture.Owner == QuestInteractionOwner.Cut && (cuts == null || !cuts.IsCaptured(index))) Cancel(hand);
            data.trackedDevicePosition = data.Ray.origin;
            data.trackedDeviceOrientation = Quaternion.LookRotation(data.Ray.direction, tracker.transform.up);
            hits.Clear();
            eventSystem.RaycastAll(data, hits);
            data.pointerCurrentRaycast = default;
            foreach (var hit in hits)
                if (hit.isValid && hit.module is TrackedDeviceRaycaster && hit.distance <= policy.RayDistance && hit.gameObject.TryGetComponent<Graphic>(out var graphic) && graphic.raycastTarget && !graphic.canvasRenderer.cull && (modalDialog == null || hit.gameObject.GetComponentInParent<QuestDialogBox>() == modalDialog) && IsUiHitAbove(hit, data.pointerCurrentRaycast))
                {
                    data.pointerCurrentRaycast = hit;
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
            bool loadingOverlay = modalDialog != null || IsLoadingOverlay(data.pointerCurrentRaycast);
            var candidate = loadingOverlay ? null : AnatomyCandidate(tracker.transform.position, target == null, data.Ray.origin);
            bool near = candidate != null;
            float distantDistance = policy.RayDistance;
            var distantCandidate = !loadingOverlay && !near && policy.EnableDistantAnatomy ? DistantAnatomyCandidate(data.Ray, out distantDistance) : null;
            bool uiFirst = target != null && (near || distantCandidate == null || data.pointerCurrentRaycast.distance <= distantDistance);
            var cutCandidate = !loadingOverlay && cuts != null ? cuts.Candidate(index) : null;
            var oldOwner = hand.Capture.Owner;
            bool began = hand.Capture.Sample(true, pressed, uiFirst, near || distantCandidate != null, near ? policy.PreferNearbyAnatomy : !uiFirst, cutCandidate != null);
            if (began && hand.Capture.Owner == QuestInteractionOwner.Cut && !cuts.Begin(index, cutCandidate)) hand.Capture.Cancel();
            if (oldOwner == QuestInteractionOwner.Cut && !pressed) cuts.End(index);
            if (hand.Capture.Owner == QuestInteractionOwner.Cut && !cuts.Move(index)) Cancel(hand);
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
                // A click control owns its trigger gesture. Do not let an ancestor ScrollRect
                // turn controller jitter into a scroll and cancel the button/toggle/dropdown.
                // Sliders and explicit drag surfaces retain their own drag handler.
                if (data.pointerPress != null && data.pointerDrag != null && data.pointerDrag != data.pointerPress && !data.pointerDrag.transform.IsChildOf(data.pointerPress.transform))
                    data.pointerDrag = null;
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

            bool actionable = target != null && (window != null || modalDialog != null || ExecuteEvents.GetEventHandler<IPointerClickHandler>(target) != null || ExecuteEvents.GetEventHandler<IDragHandler>(target) != null);
            bool distantFeedback = hand.Capture.Owner == QuestInteractionOwner.Anatomy && hand.Distant || hand.Capture.Owner == QuestInteractionOwner.None && !near && distantCandidate != null && !uiFirst;
            // Direct grabbing needs no ray; an existing UI capture keeps its feedback until release.
            bool useful = hand.Capture.Owner == QuestInteractionOwner.UI || (cutCandidate == null && !near && (distantFeedback || (hand.Capture.Owner == QuestInteractionOwner.None && actionable && uiFirst)));
            Vector3 end = data.pointerCurrentRaycast.isValid ? data.pointerCurrentRaycast.worldPosition : data.Ray.GetPoint(policy.RayDistance);
            if (distantFeedback) end = data.Ray.GetPoint(hand.Capture.Owner == QuestInteractionOwner.Anatomy ? hand.GrabDistance : distantDistance);
            Feedback(line, reticle, useful, data.Ray.origin, end, !distantFeedback, IsLoadingOverlay(data.pointerCurrentRaycast));
#if DEVELOPMENT_BUILD && UNITY_ANDROID
            if (diagnosticOutput != null)
            {
                var brains = new List<object>();
                var cutHandles = new List<object>();
                if (cuts != null)
                    foreach (var gizmo in cuts.Gizmos)
                        cutHandles.Add(new
                        {
                            cut = gizmo.Cut.ID, column = gizmo.Column.Column.ColumnData.ID,
                            gizmo.IsValid, gizmo.IsVisible, position = gizmo.Cut.Position,
                            normal = DiagnosticVector(gizmo.Cut.Normal), point = DiagnosticVector(gizmo.Cut.Point),
                            planeCenter = DiagnosticVector(gizmo.PlaneCenter),
                            gripDistanceMeters = gizmo.ContactDistance(tracker.transform.position),
                            aimDistanceMeters = gizmo.ContactDistance(data.Ray.origin),
                            span = gizmo.Scene.GetCutPositionGeometry(gizmo.Cut).Span
                        });
                if (anatomy != null)
                    foreach (var column in anatomy.AllColumns)
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
                    useful, lineEnabled = line != null && line.enabled, lineEnd = DiagnosticVector(end), brains,
                    cutsActive = cuts != null && cuts.Active, cutCandidate = cutCandidate?.Cut.ID,
                    cutColumn = cutCandidate?.Column.Column.ColumnData.ID, cutHandles,
                    uiScreen = new { data.position.x, data.position.y }, uiPressScreen = new { data.pressPosition.x, data.pressPosition.y },
                    data.eligibleForClick, data.dragging, uiDrag = data.pointerDrag?.name,
                    uiPress = data.pointerPress?.name
                };
            }
#endif
        }

        private static bool IsUiHitAbove(RaycastResult hit, RaycastResult previous)
        {
            if (!previous.isValid) return true;
            bool loading = IsLoadingOverlay(hit);
            if (loading != IsLoadingOverlay(previous)) return loading;
            var canvas = hit.module.GetComponent<Canvas>();
            var other = previous.module.GetComponent<Canvas>();
            // TrackedDeviceRaycaster does not fill sortingLayer/sortingOrder. EventSystem
            // otherwise compares a popup's local graphic depth to the window underneath.
            if (canvas != null && other != null && canvas.rootCanvas == other.rootCanvas)
            {
                int layer = SortingLayer.GetLayerValueFromID(canvas.sortingLayerID);
                int otherLayer = SortingLayer.GetLayerValueFromID(other.sortingLayerID);
                if (layer != otherLayer) return layer > otherLayer;
                if (canvas.sortingOrder != other.sortingOrder) return canvas.sortingOrder > other.sortingOrder;
                if (canvas == other && hit.depth != previous.depth) return hit.depth > previous.depth;
            }

            return hit.distance < previous.distance;
        }

        private static bool IsLoadingOverlay(RaycastResult hit) => hit.module != null && hit.module.GetComponentInParent<QuestLoadingOverlay>() is { isActiveAndEnabled: true };

#if DEVELOPMENT_BUILD && UNITY_ANDROID
        private static object DiagnosticVector(Vector3 value) => new { value.x, value.y, value.z };
#endif

        private bool HasAnatomyTarget(QuestAnatomyManipulator target)
        {
            if (target == null || anatomy == null || target.SharedMesh == null) return false;
            foreach (var column in anatomy.AllColumns)
                if (column != null && column.gameObject.activeSelf && QuestAnatomySession.CanInteractWith(column.Scene) && column.Scene.IsVisible && column.Manipulator == target)
                    return true;
            return false;
        }

        private QuestAnatomyManipulator DistantAnatomyCandidate(Ray ray, out float distance)
        {
            distance = policy.RayDistance;
            QuestAnatomyManipulator nearest = null;
            if (anatomy == null) return null;
            QuestAnatomyManipulator grabbed = null;
            foreach (var column in anatomy.AllColumns)
                if (column != null && column.gameObject.activeSelf && QuestAnatomySession.CanInteractWith(column.Scene) && column.Scene.IsVisible && anatomy.Find(column.Scene)?.SurfaceHidden != true && column.Manipulator.IsGrabbed)
                {
                    grabbed = column.Manipulator;
                    break;
                }

            foreach (var column in anatomy.AllColumns)
                if (column != null && column.gameObject.activeSelf && QuestAnatomySession.CanInteractWith(column.Scene) && column.Scene.IsVisible && anatomy.Find(column.Scene)?.SurfaceHidden != true && (cuts == null || !cuts.IsColumnLocked(column.Manipulator)) && (grabbed == null || grabbed == column.Manipulator) && column.Manipulator.Raycast(ray, distance, out float hitDistance))
                {
                    nearest = column.Manipulator;
                    distance = hitDistance;
                }

            return nearest;
        }

        private QuestAnatomyManipulator AnatomyCandidate(Vector3 position, bool allowJoining, Vector3? aimPosition = null)
        {
            if (anatomy == null) return null;
            foreach (var column in anatomy.AllColumns)
                if (column != null && column.gameObject.activeSelf && QuestAnatomySession.CanInteractWith(column.Scene) && column.Scene.IsVisible && (cuts == null || !cuts.IsColumnLocked(column.Manipulator)) && column.Manipulator.IsGrabbed)
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
            foreach (var column in anatomy.AllColumns)
                if (column != null && column.gameObject.activeSelf && QuestAnatomySession.CanInteractWith(column.Scene) && column.Scene.IsVisible && (cuts == null || !cuts.IsColumnLocked(column.Manipulator)) && (column.Manipulator.CanGrab(position) || (aimPosition.HasValue && column.Manipulator.CanGrab(aimPosition.Value))))
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

        private void Feedback(LineRenderer line, Transform reticle, bool visible, Vector3 start, Vector3 end, bool showReticle = true, bool foreground = false)
        {
            bool overlay = foreground && overlayFeedbackMaterial != null;
            if (line != null)
            {
                SetFeedbackOverlay(line, overlay);
                line.enabled = visible;
                line.positionCount = 2;
                line.useWorldSpace = true;
                line.startWidth = line.endWidth = policy != null ? policy.RayWidth : 0.002f;
                // The overlay shader multiplies vertex color by the shared feedback tint.
                if (policy != null && policy.FeedbackColor != null) line.startColor = line.endColor = overlay ? Color.white : policy.FeedbackColor.Value;
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
                var renderer = reticle.GetComponent<Renderer>();
                SetFeedbackOverlay(renderer, overlay);
                ApplyFeedbackColor(renderer);
                if (visible) reticle.position = end;
            }
        }

        private void SetFeedbackOverlay(Renderer renderer, bool overlay)
        {
            if (renderer == null) return;
            if (!feedbackDefaults.TryGetValue(renderer, out var original))
            {
                original = (renderer.sharedMaterial, renderer.sortingOrder);
                feedbackDefaults.Add(renderer, original);
            }

            renderer.sharedMaterial = overlay ? overlayFeedbackMaterial : original.Material;
            renderer.sortingOrder = overlay ? short.MaxValue : original.SortingOrder;
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
            if (cuts != null) cuts.End(hand == hands[0] ? 0 : 1);
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
            {
                if (siteProbe != null) siteProbe.Cancel();
                foreach (var hand in hands)
                    Cancel(hand);
            }
        }

        private void OnApplicationPause(bool value)
        {
            paused = value;
            if (value)
            {
                if (siteProbe != null) siteProbe.Cancel();
                foreach (var hand in hands)
                    Cancel(hand);
            }
        }

        protected override void OnDisable()
        {
            if (siteProbe != null) siteProbe.Cancel();
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
