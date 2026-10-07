using UnityEngine;

namespace HBP.Quest
{
    /// <summary>Hysteresis and delayed horizontal following; targeting always suspends motion.</summary>
    public sealed class QuestWindowFollower : MonoBehaviour
    {
        [SerializeField] private Transform head;
        [SerializeField] private QuestWindow window;
        [SerializeField, Range(5, 100)] private float exitAngle = 45;
        [SerializeField, Range(0, 90)] private float returnAngle = 15;
        [SerializeField, Min(0)] private float delay = 0.8f;
        [SerializeField, Min(0.01f)] private float speed = 2.5f;
        [SerializeField] private bool horizontalOnly;
        private float outsideTime;
        private bool returning;

        public void Configure(Transform target, QuestWindow owner)
        {
            head = target;
            window = owner;
        }

        public static Quaternion HorizontalRotation(Vector3 forward)
        {
            forward = Vector3.ProjectOnPlane(forward, Vector3.up);
            return Quaternion.LookRotation(forward.sqrMagnitude > 0.001f ? forward : Vector3.forward);
        }

        public void Recall()
        {
            if (head == null || window == null || window.IsManipulated) return;
            Quaternion yaw = DesiredRotation();
            Vector3 position = head.position + yaw * window.Placement;
            transform.SetPositionAndRotation(position, Facing(position));
            outsideTime = 0;
            returning = false;
        }

        private void LateUpdate()
        {
            if (head == null || window == null) return;
            var tracker = head.GetComponent<QuestDevicePoseTracker>();
            Step(Time.unscaledDeltaTime, tracker == null || tracker.IsTracked);
        }

        public void Step(float dt, bool tracked)
        {
            if (!tracked || head == null || window == null || !window.IsOpen || window.IsPinned || window.IsBusy)
            {
                outsideTime = 0;
                returning = false;
                return;
            }

            Quaternion yaw = DesiredRotation();
            Vector3 target = head.position + yaw * window.Placement;
            // Compare against the desired yaw, excluding pitch so looking down at the toolbar is stable.
            Vector3 currentDirection = transform.position - head.position;
            Vector3 targetDirection = target - head.position;
            if (horizontalOnly)
            {
                currentDirection = Vector3.ProjectOnPlane(currentDirection, Vector3.up);
                targetDirection = Vector3.ProjectOnPlane(targetDirection, Vector3.up);
            }

            float angle = Vector3.Angle(currentDirection, targetDirection);
            if (!returning)
            {
                if (angle <= exitAngle)
                {
                    outsideTime = 0;
                    return;
                }

                outsideTime += Mathf.Max(0, dt);
                if (outsideTime < delay) return;
                returning = true;
            }

            float blend = 1 - Mathf.Exp(-speed * Mathf.Max(0, dt));
            transform.SetPositionAndRotation(Vector3.Lerp(transform.position, target, blend), Quaternion.Slerp(transform.rotation, Facing(target), blend));
            if (angle <= Mathf.Min(returnAngle, exitAngle - 1))
            {
                returning = false;
                outsideTime = 0;
            }
        }

        private Quaternion DesiredRotation() => horizontalOnly ? HorizontalRotation(head.forward) : Quaternion.LookRotation(head.forward, Vector3.up);
        private Quaternion Facing(Vector3 position) => horizontalOnly ? HorizontalRotation(position - head.position) : Quaternion.LookRotation(position - head.position, Vector3.up);
    }
}
