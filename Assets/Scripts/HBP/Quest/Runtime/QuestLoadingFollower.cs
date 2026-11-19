using UnityEngine;

namespace HBP.Quest
{
    public sealed class QuestLoadingFollower : MonoBehaviour
    {
        [SerializeField] private Camera headCamera;
        [SerializeField] private GameObject content;
        [SerializeField, Min(.1f)] private float distance = 1.15f;
        [SerializeField, Range(5, 90)] private float exitAngle = 25;
        [SerializeField, Range(0, 45)] private float returnAngle = 8;
        [SerializeField, Min(0)] private float delay = .4f;
        [SerializeField, Min(.01f)] private float speed = 3;
        private bool placed, returning;
        private float outsideTime;

        public void SetCamera(Camera value)
        {
            headCamera = value;
            placed = false;
        }

        private void LateUpdate()
        {
            if (headCamera == null) return;
            var canvas = GetComponent<Canvas>();
            if (canvas != null && canvas.worldCamera != headCamera) canvas.worldCamera = headCamera;
            var tracker = headCamera.GetComponent<QuestDevicePoseTracker>();
            Step(Time.unscaledDeltaTime, tracker == null || tracker.IsTracked);
        }

        public void Step(float dt, bool tracked)
        {
            if (content == null || !content.activeInHierarchy)
            {
                placed = returning = false;
                outsideTime = 0;
                return;
            }

            if (!tracked || headCamera == null)
            {
                returning = false;
                outsideTime = 0;
                return;
            }

            var head = headCamera.transform;
            var rotation = Quaternion.LookRotation(head.forward, Vector3.up);
            var target = head.position + head.forward * distance;
            if (!placed)
            {
                transform.SetPositionAndRotation(target, rotation);
                placed = true;
                return;
            }

            float angle = Vector3.Angle(transform.position - head.position, head.forward);
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
            transform.SetPositionAndRotation(Vector3.Lerp(transform.position, target, blend), Quaternion.Slerp(transform.rotation, rotation, blend));
            if (angle <= Mathf.Min(returnAngle, exitAngle - 1))
            {
                returning = false;
                outsideTime = 0;
            }
        }
    }
}
