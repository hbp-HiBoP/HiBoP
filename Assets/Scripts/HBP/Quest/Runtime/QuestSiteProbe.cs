using HBP.Core.Tools;
using System.Collections.Generic;
using HBP.Data.Module3D;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using Site = HBP.Core.Object3D.Site;

namespace HBP.Quest
{
    /// <summary>Right-hand A tool. The universal pointer owns arbitration; contacts and setters remain separate.</summary>
    public sealed class QuestSiteProbe : MonoBehaviour
    {
        [SerializeField] private QuestAnatomyView anatomy;
        [SerializeField] private QuestInteractionPolicy policy;
        [SerializeField] private QuestDevicePoseTracker right;
        [SerializeField] private Transform marker;
        [SerializeField] private Renderer markerRenderer;
        [SerializeField] private bool usePreferredSelection;
        private QuestSelectionSettings preferredSettings;
        private readonly QuestSiteContactPolicy contactPolicy = new();
        private readonly List<QuestSiteTarget> targets = new();
        private readonly Dictionary<Site, QuestSiteTarget> cache = new();
        private InputAction activate;
        private Base3DScene capturedScene;
        private QuestSiteTarget lastContact;
        private bool armed;
        private MaterialPropertyBlock color;
        public QuestSelectionSettings ExperimentSettings { get; set; }
        public QuestSelectionSettings Settings => ExperimentSettings ?? preferredSettings;
        public QuestSiteTarget CurrentContact => lastContact;
        public Vector3 ProbePosition { get; private set; }
        public Pose CursorPose { get; private set; }
        public QuestSiteAssistance Assistance { get; } = new();
        public long StabilizedFrameCount { get; private set; }
        public bool IsActive { get; private set; }
        public GenericEvent<Site> OnSelected { get; } = new();

        private void OnEnable()
        {
            preferredSettings = usePreferredSelection ? QuestSelectionSettings.Default() : null;
            activate = new InputAction("Quest site probe (A)", InputActionType.Button, "<XRController>{RightHand}/primaryButton");
            activate.Enable();
            if (anatomy != null) anatomy.OnPresentationChanging.AddListener(OnPresentationChanging);
            color = new MaterialPropertyBlock();
            Cancel();
        }

        public bool Process(bool valid, bool handBusy, Ray aim)
        {
            if (!isActiveAndEnabled) return false;
            bool pressed = activate.IsPressed();
            var scene = capturedScene;
            if (!valid || policy == null || right == null || marker == null || anatomy == null || anatomy.Scenes.Count == 0 || capturedScene != null && (!QuestAnatomySession.CanInteractWith(capturedScene) || !capturedScene.IsVisible || anatomy.Find(capturedScene) == null))
            {
                Cancel();
                return false;
            }

            if (!pressed)
            {
                Stop();
                armed = true;
                return false;
            }

            if (!IsActive)
            {
                if (!armed) return false;
                armed = false;
                if (handBusy) return false; // A held during an existing capture needs a fresh press.
                IsActive = true;
                capturedScene = null;
                cache.Clear();
            }

            var settings = Settings;
            CursorPose = ResolveCursorPose(aim, right.transform.up, settings?.CursorOffset ?? Vector3.zero);
            Vector3 point = CursorPose.position;
            ProbePosition = point;
            marker.SetPositionAndRotation(point, CursorPose.rotation);
            marker.localScale = Vector3.one * (2 * (settings?.MarkerRadius ?? policy.SiteProbeRadius));
            marker.gameObject.SetActive(true);
            if (markerRenderer != null && policy.FeedbackColor != null)
            {
                color.SetColor("_BaseColor", policy.FeedbackColor.Value);
                color.SetColor("_Color", policy.FeedbackColor.Value);
                markerRenderer.SetPropertyBlock(color);
            }

            targets.Clear();
            foreach (var column in anatomy.AllColumns)
                if (anatomy.CanInteractWithColumn(column) && column.Column != null && (capturedScene == null || column.Scene == capturedScene))
                    foreach (var site in column.Column.Sites)
                    {
                        if (site == null) continue;
                        if (!cache.TryGetValue(site, out var target)) cache.Add(site, target = new QuestSiteTarget(column.Column, site));
                        if (target.IsSelectable(column.Scene)) targets.Add(target);
                    }

            Vector3 viewAxis = Camera.main != null ? point - Camera.main.transform.position : aim.direction;
            var contact = settings == null ? contactPolicy.FindContact(point, policy.SiteProbeRadius, targets) : Assistance.Find(point, viewAxis, new Ray(point, aim.direction), settings, targets, lastContact);
            if (settings != null && Assistance.Retained) StabilizedFrameCount++;
            if (contact != lastContact)
            {
                lastContact = contact;
                var owner = contact == null ? null : System.Linq.Enumerable.FirstOrDefault(anatomy.AllColumns, value => value.Column == contact.Column);
                if (owner != null)
                {
                    capturedScene = owner.Scene;
                    scene = owner.Scene;
                    anatomy.SelectContext(owner.Scene, owner.Column);
                }

                if (owner != null && QuestSiteSelection.TrySelect(scene, contact))
                {
                    OnSelected.Invoke(contact.Site);
                    var device = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
                    if ((settings?.Haptics ?? true) && device.TryGetHapticCapabilities(out var capabilities) && capabilities.supportsImpulse)
                        device.SendHapticImpulse(0, settings?.HapticAmplitude ?? policy.SiteHapticAmplitude, policy.SiteHapticDuration);
                }
            }

            return true;
        }

        /// <summary>Physical metres in the aim frame: X right, Y up, Z forward. Controller roll defines up.</summary>
        public static Pose ResolveCursorPose(Ray aim, Vector3 up, Vector3 offset)
        {
            Vector3 forward = aim.direction.normalized;
            if (forward == Vector3.zero) forward = Vector3.forward;
            up = Vector3.ProjectOnPlane(up, forward).normalized;
            if (up == Vector3.zero) up = Vector3.ProjectOnPlane(Mathf.Abs(forward.y) < .99f ? Vector3.up : Vector3.right, forward).normalized;
            Quaternion rotation = Quaternion.LookRotation(forward, up);
            return new Pose(aim.origin + rotation * offset, rotation);
        }

        private void Stop()
        {
            IsActive = false;
            capturedScene = null;
            lastContact = null;
            if (marker != null) marker.gameObject.SetActive(false);
            cache.Clear();
        }

        public void Cancel()
        {
            Stop();
            armed = false;
        }

        private void OnPresentationChanging(Base3DScene scene)
        {
            if (capturedScene == null || capturedScene == scene) Cancel();
        }

        private void OnDisable()
        {
            if (anatomy != null) anatomy.OnPresentationChanging.RemoveListener(OnPresentationChanging);
            Cancel();
            activate?.Dispose();
            activate = null;
        }
    }
}
