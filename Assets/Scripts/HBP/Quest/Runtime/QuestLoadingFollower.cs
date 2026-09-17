using UnityEngine;

namespace HBP.Quest
{
    public sealed class QuestLoadingFollower : MonoBehaviour
    {
        [SerializeField] private Camera headCamera;

        public void SetCamera(Camera value) => headCamera = value;

        private void LateUpdate()
        {
            if (headCamera == null) return;
            transform.SetPositionAndRotation(headCamera.transform.position + headCamera.transform.forward * 2f, headCamera.transform.rotation);
        }
    }
}
