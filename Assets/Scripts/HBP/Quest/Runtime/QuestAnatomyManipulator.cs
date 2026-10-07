using UnityEngine;

namespace HBP.Quest
{
    /// <summary>Local presentation only. Never writes to the anatomical frame or snapshot buffers.</summary>
    public sealed class QuestAnatomyManipulator : MonoBehaviour
    {
        private HBP.Data.Module3D.Column3D column;
        [SerializeField] private MeshCollider rayTarget;
        private bool rayTargetDirty;
        public void InvalidateRayTarget() => rayTargetDirty = true;
        public Mesh SharedMesh => column != null && column.BrainMesh != null ? column.BrainMesh.GetComponent<MeshFilter>().sharedMesh : null;

        public void Bind(HBP.Data.Module3D.Column3D value)
        {
            column = value;
            if (rayTarget != null && column != null && column.BrainMesh != null)
            {
                rayTarget.transform.SetParent(column.BrainMesh.transform, false);
                rayTarget.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                rayTarget.transform.localScale = Vector3.one;
                rayTarget.sharedMesh = SharedMesh;
            }

            CancelGrab();
        }

        [SerializeField, Min(0.01f)] private float minimumScale = 0.25f;
        [SerializeField, Min(0.01f)] private float maximumScale = 4f;
        [SerializeField, Min(0.01f)] private float minimumHandDistanceMeters = 0.08f;
        [SerializeField, Min(0.1f)] private float recenterDistanceMeters = 0.65f;
        [SerializeField] private float recenterHeightMeters = -0.12f;
        [SerializeField] private Vector3 recenterEuler = new Vector3(-90, 0, 0);
        private int hands;
        private bool leftHeld, rightHeld;
        private bool leftBlocked = true, rightBlocked = true;
        private Pose anchorHand, anchorGroup;
        private Vector3 anchorSpan;
        private float anchorScale;
        private bool pairReady;

        public bool IsGrabbed => hands != 0;

        private void OnValidate()
        {
            minimumScale = Mathf.Max(0.01f, minimumScale);
            maximumScale = Mathf.Max(minimumScale, maximumScale);
            minimumHandDistanceMeters = Mathf.Max(0.01f, minimumHandDistanceMeters);
            recenterDistanceMeters = Mathf.Max(0.1f, recenterDistanceMeters);
        }

        public void CancelGrab()
        {
            hands = 0;
            leftBlocked = rightBlocked = true;
        }

        private void OnDisable() => CancelGrab();

        /// <summary>Sample once after tracking. A new press near the surface starts a grab.</summary>
        public void Step(Pose left, bool leftTracked, bool leftGrip, Pose right, bool rightTracked, bool rightGrip, bool leftDistant = false, bool rightDistant = false)
        {
            if (!leftTracked) leftBlocked = true;
            else if (!leftGrip) leftBlocked = false;
            if (!rightTracked) rightBlocked = true;
            else if (!rightGrip) rightBlocked = false;
            bool leftPress = leftTracked && !leftBlocked && leftGrip && !leftHeld;
            bool rightPress = rightTracked && !rightBlocked && rightGrip && !rightHeld;
            leftHeld = leftGrip;
            rightHeld = rightGrip;
            if (SharedMesh == null)
            {
                CancelGrab();
                return;
            }

            int next = hands;
            if (!leftTracked || !leftGrip) next &= ~1;
            if (!rightTracked || !rightGrip) next &= ~2;
            if (leftPress && (next != 0 || leftDistant || IsNear(left.position))) next |= 1;
            if (rightPress && (next != 0 || rightDistant || IsNear(right.position))) next |= 2;
            if (next != hands)
            {
                hands = next;
                Anchor(left, right); // No snapping when entering/leaving a two-controller gesture.
                return;
            }

            if (hands == 0) return;
            if (hands == 3)
            {
                Vector3 span = right.position - left.position;
                if (span.magnitude < minimumHandDistanceMeters)
                {
                    pairReady = false; // Coincident controllers cannot define a stable rotation or ratio.
                    return;
                }

                if (!pairReady)
                {
                    Anchor(left, right);
                    return;
                }

                Quaternion rotation = Quaternion.FromToRotation(anchorSpan, span);
                float scale = Mathf.Clamp(anchorScale * span.magnitude / anchorSpan.magnitude, minimumScale, maximumScale);
                transform.localScale = Vector3.one * scale;
                transform.SetPositionAndRotation((left.position + right.position) * 0.5f + rotation * (anchorGroup.position - anchorHand.position) * (scale / anchorScale), rotation * anchorGroup.rotation);
            }
            else
            {
                Pose hand = hands == 1 ? left : right;
                Quaternion rotation = hand.rotation * Quaternion.Inverse(anchorHand.rotation);
                transform.SetPositionAndRotation(hand.position + rotation * (anchorGroup.position - anchorHand.position), rotation * anchorGroup.rotation);
            }
        }

        private void Anchor(Pose left, Pose right)
        {
            anchorGroup = new Pose(transform.position, transform.rotation);
            anchorScale = transform.localScale.x;
            anchorSpan = right.position - left.position;
            anchorHand = hands == 3 ? new Pose((left.position + right.position) * 0.5f, Quaternion.identity) : hands == 1 ? left : right;
            pairReady = anchorSpan.magnitude >= minimumHandDistanceMeters;
        }

        public bool CanGrab(Vector3 position) => SharedMesh != null && IsNear(position);
        public Vector3 GrabCenter => column.BrainMesh.transform.TransformPoint(SharedMesh.bounds.center);

        public bool Raycast(Ray ray, float maximumDistance, out float distance)
        {
            distance = maximumDistance;
            if (column == null || column.BrainMesh == null || !column.BrainMesh.activeInHierarchy) return false;
            var renderer = column.BrainMesh.GetComponent<Renderer>();
            if (renderer != null && (!renderer.enabled || renderer.forceRenderingOff)) return false;
            var collider = rayTarget;
            if (collider != null && (rayTargetDirty || collider.sharedMesh != SharedMesh))
            {
                collider.sharedMesh = null;
                collider.sharedMesh = SharedMesh;
                rayTargetDirty = false;
            }

            if (collider == null || !collider.enabled || !collider.Raycast(ray, out var hit, maximumDistance)) return false;
            distance = hit.distance;
            return true;
        }

        private bool IsNear(Vector3 position)
        {
            return SharedMesh.bounds.Contains(column.BrainMesh.transform.InverseTransformPoint(position));
        }

        public Vector3 ClosestGrabPoint(Vector3 position)
        {
            if (SharedMesh == null) return position;
            // Use the mesh's actual transform, including presentation rotation and scale.
            var frame = column.BrainMesh.transform;
            return frame.TransformPoint(SharedMesh.bounds.ClosestPoint(frame.InverseTransformPoint(position)));
        }

        public void Recenter(Pose head)
        {
            CancelGrab();
            if (SharedMesh == null) return;
            Vector3 forward = Vector3.ProjectOnPlane(head.rotation * Vector3.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.5f) forward = Vector3.forward;
            Quaternion rotation = Quaternion.LookRotation(forward) * Quaternion.Euler(recenterEuler);
            Vector3 center = head.position + forward * recenterDistanceMeters + Vector3.up * recenterHeightMeters;
            transform.rotation = rotation;
            // Keep the chosen scale, surface, landmarks and any future sites in the same parent.
            transform.position = center - transform.TransformVector(SharedMesh.bounds.center * 0.001f);
        }
    }
}
