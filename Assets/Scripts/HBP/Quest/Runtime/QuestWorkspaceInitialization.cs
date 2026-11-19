using UnityEngine;

namespace HBP.Quest
{
    /// <summary>Wait for continuous presence/tracking and settling; never infer presence from tracking alone.</summary>
    public sealed class QuestWorkspaceInitialization
    {
        private float elapsed, stable;
        private Pose sample;
        private bool sampled;

        public void Reset()
        {
            elapsed = stable = 0;
            sampled = false;
        }

        public bool Step(float deltaTime, bool available, Pose head)
        {
            var forward = Vector3.ProjectOnPlane(head.rotation * Vector3.forward, Vector3.up);
            if (!available || forward.sqrMagnitude < 0.001f)
            {
                Reset();
                return false;
            }

            var pose = new Pose(head.position, Quaternion.LookRotation(forward));
            if (!sampled)
            {
                // This frame's delta may include time spent suspended or before the headset was worn.
                sample = pose;
                sampled = true;
                return false;
            }

            if (Vector3.Distance(sample.position, pose.position) > 0.03f || Quaternion.Angle(sample.rotation, pose.rotation) > 5)
            {
                sample = pose;
                stable = 0;
            }
            else
            {
                stable += Mathf.Max(0, deltaTime);
            }

            elapsed += Mathf.Max(0, deltaTime);
            // One second to adjust the headset, then a stable second; moving users wait at most five seconds.
            return (elapsed >= 2 && stable >= 1) || elapsed >= 5;
        }
    }
}
