using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace HBP.Quest
{
    /// <summary>Tracked-controller input for the authored pairing card. No scene interaction is intercepted.</summary>
    public sealed class QuestPairingUIInput : MonoBehaviour
    {
        [SerializeField] private InputSystemUIInputModule module;
        [SerializeField] private Transform trackingOrigin;
        [SerializeField] private GameObject pairingCard;
        [SerializeField] private LineRenderer pointer;
        [SerializeField] private QuestDevicePoseTracker rightController;
        private InputAction position, rotation, select;
        private InputActionAsset actions;
        private InputActionReference positionReference, rotationReference, selectReference;

        private void Update()
        {
            // Tracking has its own actions, so disabling UI input cannot prevent it from waking again.
            bool visible = pairingCard.activeInHierarchy && rightController.IsTracked;
            module.enabled = visible;
            pointer.enabled = visible && position.activeControl != null;
            if (!pointer.enabled) return;
            Vector3 start = trackingOrigin.TransformPoint(position.ReadValue<Vector3>());
            Vector3 direction = trackingOrigin.rotation * rotation.ReadValue<Quaternion>() * Vector3.forward;
            var hit = module.GetLastRaycastResult(position.activeControl.device.deviceId);
            pointer.SetPosition(0, start);
            pointer.SetPosition(1, hit.isValid ? hit.worldPosition : start + direction * 2);
        }

        private void OnEnable()
        {
            actions = ScriptableObject.CreateInstance<InputActionAsset>();
            var map = new InputActionMap("Pairing");
            actions.AddActionMap(map);
            position = map.AddAction("Pairing pointer position", InputActionType.PassThrough, "<XRController>{RightHand}/pointerPosition", expectedControlLayout: "Vector3");
            rotation = map.AddAction("Pairing pointer rotation", InputActionType.PassThrough, "<XRController>{RightHand}/pointerRotation", expectedControlLayout: "Quaternion");
            select = map.AddAction("Select pairing action", InputActionType.PassThrough, "<XRController>{RightHand}/trigger", expectedControlLayout: "Button");
            positionReference = InputActionReference.Create(position);
            rotationReference = InputActionReference.Create(rotation);
            selectReference = InputActionReference.Create(select);
            module.xrTrackingOrigin = trackingOrigin;
            module.trackedDevicePosition = positionReference;
            module.trackedDeviceOrientation = rotationReference;
            module.leftClick = selectReference;
            position.Enable();
            rotation.Enable();
            select.Enable();
        }

        private void OnDisable()
        {
            module.enabled = false;
            pointer.enabled = false;
            module.trackedDevicePosition = null;
            module.trackedDeviceOrientation = null;
            module.leftClick = null;
            position?.Dispose();
            rotation?.Dispose();
            select?.Dispose();
            if (positionReference) Destroy(positionReference);
            if (rotationReference) Destroy(rotationReference);
            if (selectReference) Destroy(selectReference);
            if (actions) Destroy(actions);
            actions = null;
        }
    }
}
