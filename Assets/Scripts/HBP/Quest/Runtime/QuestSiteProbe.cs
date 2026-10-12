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
        private readonly QuestSiteContactPolicy contactPolicy = new();
        private readonly List<QuestSiteTarget> targets = new();
        private readonly Dictionary<Site, QuestSiteTarget> cache = new();
        private InputAction activate;
        private Base3DScene capturedScene;
        private QuestSiteTarget lastContact;
        private bool armed;
        private MaterialPropertyBlock color;
        public bool IsActive { get; private set; }
        public GenericEvent<Site> OnSelected { get; } = new();

        private void OnEnable()
        {
            activate = new InputAction("Quest site probe (A)", InputActionType.Button, "<XRController>{RightHand}/primaryButton");
            activate.Enable();
            color = new MaterialPropertyBlock();
            Cancel();
        }

        public bool Process(bool valid, bool handBusy, Ray aim)
        {
            if (!isActiveAndEnabled) return false;
            bool pressed = activate.IsPressed();
            var scene = capturedScene;
            if (!valid || policy == null || right == null || marker == null || anatomy == null || anatomy.Scenes.Count == 0 || capturedScene != null && (capturedScene.IsClosing || !capturedScene.IsVisible || anatomy.Find(capturedScene) == null))
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

            Vector3 point = aim.origin;
            marker.SetPositionAndRotation(point, Quaternion.identity);
            marker.localScale = Vector3.one * (2 * policy.SiteProbeRadius);
            marker.gameObject.SetActive(true);
            if (markerRenderer != null && policy.FeedbackColor != null)
            {
                color.SetColor("_BaseColor", policy.FeedbackColor.Value);
                color.SetColor("_Color", policy.FeedbackColor.Value);
                markerRenderer.SetPropertyBlock(color);
            }

            targets.Clear();
            foreach (var column in anatomy.AllColumns)
                if (column != null && column.Column != null && column.Scene.IsVisible && (capturedScene == null || column.Scene == capturedScene))
                    foreach (var site in column.Column.Sites)
                    {
                        if (site == null) continue;
                        if (!cache.TryGetValue(site, out var target)) cache.Add(site, target = new QuestSiteTarget(column.Column, site));
                        if (target.IsSelectable(column.Scene)) targets.Add(target);
                    }

            var contact = contactPolicy.FindContact(point, policy.SiteProbeRadius, targets);
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
                    if (device.TryGetHapticCapabilities(out var capabilities) && capabilities.supportsImpulse)
                        device.SendHapticImpulse(0, policy.SiteHapticAmplitude, policy.SiteHapticDuration);
                }
            }

            return true;
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

        private void OnDisable()
        {
            Cancel();
            activate?.Dispose();
            activate = null;
        }
    }
}
