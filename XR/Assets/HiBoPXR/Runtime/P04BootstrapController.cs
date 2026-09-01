using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CRNL.HiBoP.XR.Bootstrap
{
    public sealed class P04BootstrapController : MonoBehaviour
    {
        private const int PassthroughStartupGraceFrames = 120;

        [SerializeField] private MonoBehaviour passthroughProviderBehaviour;

        [SerializeField] private P04DevicePoseTracker head;

        [SerializeField] private P04DevicePoseTracker leftController;

        [SerializeField] private P04DevicePoseTracker rightController;

        [SerializeField] private P04HandWristTracker leftHand;

        [SerializeField] private P04HandWristTracker rightHand;

        [SerializeField] private TextMesh statusText;

        private InputAction togglePassthroughAction;
        private IPassthroughProvider passthroughProvider;
        private bool passthroughRequested;
        private float nextStatusUpdate;

        public void Configure(MonoBehaviour provider, P04DevicePoseTracker configuredHead, P04DevicePoseTracker configuredLeftController, P04DevicePoseTracker configuredRightController, P04HandWristTracker configuredLeftHand, P04HandWristTracker configuredRightHand, TextMesh configuredStatusText)
        {
            passthroughProviderBehaviour = provider;
            head = configuredHead;
            leftController = configuredLeftController;
            rightController = configuredRightController;
            leftHand = configuredLeftHand;
            rightHand = configuredRightHand;
            statusText = configuredStatusText;
        }

        private void Awake()
        {
            passthroughProvider = passthroughProviderBehaviour as IPassthroughProvider;
            if (passthroughProvider == null)
            {
                Debug.LogError("P04 bootstrap has no valid passthrough provider.", this);
            }

            togglePassthroughAction = new InputAction("P04 toggle passthrough", InputActionType.Button);
            togglePassthroughAction.AddBinding("<XRController>{LeftHand}/primaryButton");
            togglePassthroughAction.AddBinding("<XRController>{RightHand}/primaryButton");
            togglePassthroughAction.performed += OnTogglePassthrough;
            togglePassthroughAction.Enable();
        }

        private IEnumerator Start()
        {
            passthroughRequested = passthroughProvider != null && passthroughProvider.TrySetPassthrough(true);
            Debug.Log($"P04 environment mode requested: {(passthroughRequested ? "passthrough" : "VR")}.", this);

            for (int frame = 0; frame < PassthroughStartupGraceFrames && passthroughRequested; frame++)
            {
                if (passthroughProvider.IsPassthroughActive)
                {
                    yield break;
                }

                yield return null;
            }

            if (passthroughRequested && !passthroughProvider.IsPassthroughActive)
            {
                passthroughRequested = false;
                passthroughProvider.TrySetPassthrough(false);
                Debug.LogWarning("P04 passthrough unavailable; continuing in VR fallback.", this);
            }
        }

        private void OnDestroy()
        {
            if (togglePassthroughAction == null)
            {
                return;
            }

            togglePassthroughAction.performed -= OnTogglePassthrough;
            togglePassthroughAction.Dispose();
        }

        private void Update()
        {
            if (statusText == null || Time.unscaledTime < nextStatusUpdate)
            {
                return;
            }

            nextStatusUpdate = Time.unscaledTime + 0.25f;
            string environment = passthroughProvider != null && passthroughProvider.IsPassthroughActive ? "PASSTHROUGH" : "VR";
            statusText.text = $"HiBoP XR P04 | {environment}\n" + $"Head: {State(head?.IsTracked)}\n" + $"Hands L/R: {State(leftHand?.IsTracked)} / {State(rightHand?.IsTracked)}\n" + $"Controllers L/R: {State(leftController?.IsTracked)} / {State(rightController?.IsTracked)}\n" + "Press A or X to toggle passthrough";
        }

        private void OnTogglePassthrough(InputAction.CallbackContext context)
        {
            if (passthroughProvider == null)
            {
                return;
            }

            bool requested = !passthroughRequested;
            passthroughRequested = passthroughProvider.TrySetPassthrough(requested) && requested;
            if (passthroughRequested && !passthroughProvider.IsAvailable)
            {
                passthroughRequested = false;
                passthroughProvider.TrySetPassthrough(false);
            }

            Debug.Log($"P04 environment mode requested: {(passthroughRequested ? "passthrough" : "VR")}.", this);
        }

        private static string State(bool? tracked)
        {
            return tracked == true ? "TRACKED" : "not tracked";
        }
    }
}
