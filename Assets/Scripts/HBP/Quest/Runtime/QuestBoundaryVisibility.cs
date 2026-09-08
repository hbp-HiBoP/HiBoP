using UnityEngine;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Meta;
using UnityEngine.XR.OpenXR.NativeTypes;

namespace HBP.Quest
{
    /// <summary>Request free movement only while this app presents the physical room.</summary>
    public sealed class QuestBoundaryVisibility : MonoBehaviour
    {
        [SerializeField] private QuestPassthroughStatus passthrough;
        [SerializeField] private bool freeMovementInPassthrough = true;
        private BoundaryVisibilityFeature feature;
        private bool focused = true;
        private bool paused;
        private bool recenteringConfigured;
        private XrBoundaryVisibility? acceptedRequest;
        private XrBoundaryVisibility? lastRequested;
        private XrBoundaryVisibility? lastReported;
        private XrResult? lastResult;
        private float retryAt;

        public void Configure(QuestPassthroughStatus provider) => passthrough = provider;

        private void Update()
        {
            if (!OpenXRRuntime.IsExtensionEnabled("XR_META_boundary_visibility"))
            {
                feature = null;
                acceptedRequest = lastRequested = lastReported = null;
                recenteringConfigured = false;
                return;
            }

            var available = OpenXRSettings.Instance?.GetFeature<BoundaryVisibilityFeature>();
            if (available == null || !available.enabled) return;
            if (feature != available)
            {
                feature = available;
                acceptedRequest = lastRequested = lastReported = null;
                retryAt = 0;
            }

            if (!recenteringConfigured)
            {
                // Floor + recentering uses local-floor space instead of a boundary-dependent Stage.
                OpenXRSettings.SetAllowRecentering(true);
                recenteringConfigured = true;
            }

            if (lastReported != feature.currentVisibility)
            {
                lastReported = feature.currentVisibility;
                Debug.Log($"QUEST-008 boundary actual={feature.currentVisibility}", this);
            }

            bool suppress = freeMovementInPassthrough && focused && !paused && passthrough != null && passthrough.GetProblem() == null;
            Request(suppress, false);
        }

        private void Request(bool suppress, bool immediate)
        {
            if (feature == null || !feature.enabled || !OpenXRRuntime.IsExtensionEnabled("XR_META_boundary_visibility")) return;
            var desired = suppress ? XrBoundaryVisibility.VisibilitySuppressed : XrBoundaryVisibility.VisibilityNotSuppressed;
            if (!immediate && acceptedRequest == desired && feature.currentVisibility == desired) return;
            if (!immediate && lastRequested == desired && Time.unscaledTime < retryAt) return;
            bool changed = lastRequested != desired;
            lastRequested = desired;
            retryAt = Time.unscaledTime + 1f;
            XrResult result = feature.TryRequestBoundaryVisibility(desired);
            // A positive SUPPRESSION_NOT_ALLOWED result is a refusal, not XR_SUCCESS.
            if (result == XrResult.Success) acceptedRequest = desired;
            if (changed || lastResult != result)
                Debug.Log($"QUEST-008 boundary request={desired}; result={result}; actual={feature.currentVisibility}", this);
            lastResult = result;
        }

        private void OnApplicationFocus(bool value)
        {
            focused = value;
            retryAt = 0;
            if (value) acceptedRequest = lastRequested = null;
            if (!value) Request(false, true);
        }

        private void OnApplicationPause(bool value)
        {
            paused = value;
            retryAt = 0;
            if (!value) acceptedRequest = lastRequested = null;
            if (value) Request(false, true);
        }

        private void OnDisable()
        {
            // Restoration must also work before the asynchronous suppression event arrives.
            Request(false, true);
            feature = null;
            acceptedRequest = lastRequested = lastReported = null;
            retryAt = 0;
        }

        private void OnApplicationQuit() => Request(false, true);
    }
}
