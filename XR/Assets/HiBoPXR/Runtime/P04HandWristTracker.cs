using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;

namespace CRNL.HiBoP.XR.Bootstrap
{
    public sealed class P04HandWristTracker : MonoBehaviour
    {
        private static readonly List<XRHandSubsystem> HandSubsystems = new();

        [SerializeField] private Handedness handedness;

        [SerializeField] private Transform poseTarget;

        [SerializeField] private Renderer diagnosticRenderer;

        private XRHandSubsystem handSubsystem;

        public bool IsTracked { get; private set; }

        public Handedness Handedness => handedness;

        public void Configure(Handedness configuredHandedness, Transform target, Renderer marker)
        {
            handedness = configuredHandedness;
            poseTarget = target;
            diagnosticRenderer = marker;
        }

        private void Update()
        {
            if (handSubsystem == null || !handSubsystem.running)
            {
                FindRunningSubsystem();
            }

            XRHand hand = handedness == Handedness.Left ? handSubsystem?.leftHand ?? default : handSubsystem?.rightHand ?? default;

            Pose wristPose = default;
            IsTracked = hand.isTracked && hand.GetJoint(XRHandJointID.Wrist).TryGetPose(out wristPose);

            if (diagnosticRenderer != null)
            {
                diagnosticRenderer.enabled = IsTracked;
            }

            if (IsTracked && poseTarget != null)
            {
                poseTarget.SetLocalPositionAndRotation(wristPose.position, wristPose.rotation);
            }
        }

        private void FindRunningSubsystem()
        {
            HandSubsystems.Clear();
            SubsystemManager.GetSubsystems(HandSubsystems);
            handSubsystem = HandSubsystems.Find(subsystem => subsystem.running);
        }
    }
}
