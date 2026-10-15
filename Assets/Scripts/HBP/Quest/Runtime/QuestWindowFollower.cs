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
        [SerializeField] private bool tiltTowardHead;
        [SerializeField, Min(0)] private float horizontalThreshold = 0.05f;
        [SerializeField, Min(0)] private float verticalThreshold = 0.04f;
        private float outsideTime;
        private bool returning;
        private bool railInitialized, movingCenter;
        private Vector3 railCenter;
        private Quaternion railRotation = Quaternion.identity, headYaw = Quaternion.identity;

        public void Configure(Transform target, QuestWindow owner)
        {
            head = target;
            window = owner;
            railInitialized = false;
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
            railCenter = head.position;
            railRotation = yaw;
            railInitialized = true;
            movingCenter = false;
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
                movingCenter = false;
                return;
            }

            if (horizontalOnly)
            {
                StepRail(dt);
                return;
            }

            Quaternion yaw = DesiredRotation();
            Vector3 target = head.position + yaw * window.Placement;
            Vector3 currentDirection = transform.position - head.position;
            Vector3 targetDirection = target - head.position;
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

        private void StepRail(float dt)
        {
            if (!railInitialized) Recall();
            Quaternion targetRotation = DesiredRotation();
            float blend = 1 - Mathf.Exp(-speed * Mathf.Max(0, dt));
            Vector3 offset = head.position - railCenter;
            if (Vector3.ProjectOnPlane(offset, Vector3.up).magnitude > horizontalThreshold || Mathf.Abs(offset.y) > verticalThreshold)
                movingCenter = true;
            if (movingCenter)
            {
                railCenter = Vector3.Lerp(railCenter, head.position, blend);
                if (Vector3.Distance(railCenter, head.position) < 0.005f) movingCenter = false;
            }

            float angle = Quaternion.Angle(railRotation, targetRotation);
            if (!returning)
            {
                if (angle <= exitAngle) outsideTime = 0;
                else
                {
                    outsideTime += Mathf.Max(0, dt);
                    returning = outsideTime >= delay;
                }
            }

            if (returning)
            {
                // Interpolate the angle, never the chord between two points on the rail.
                railRotation = Quaternion.Slerp(railRotation, targetRotation, blend);
                if (Quaternion.Angle(railRotation, targetRotation) <= Mathf.Min(returnAngle, exitAngle - 1))
                {
                    returning = false;
                    outsideTime = 0;
                }
            }

            Vector3 placement = railRotation * window.Placement;
            transform.SetPositionAndRotation(railCenter + placement, RailFacing(placement));
        }

        private Quaternion DesiredRotation()
        {
            if (!horizontalOnly) return Quaternion.LookRotation(head.forward, Vector3.up);
            Vector3 forward = Vector3.ProjectOnPlane(head.forward, Vector3.up);
            // At the poles yaw is undefined. Keep the last valid heading instead of snapping north.
            if (forward.sqrMagnitude > 0.001f) headYaw = Quaternion.LookRotation(forward, Vector3.up);
            return headYaw;
        }

        // Face the filtered rail center, so pitch/roll and small head translations cannot wobble the panel.
        private Quaternion RailFacing(Vector3 placement) => tiltTowardHead ? Quaternion.LookRotation(placement, Vector3.up) : HorizontalRotation(placement);
        private Quaternion Facing(Vector3 position) => horizontalOnly ? RailFacing(position - head.position) : Quaternion.LookRotation(position - head.position, Vector3.up);
    }
}
