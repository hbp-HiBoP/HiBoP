using UnityEngine;

namespace HBP.Rendering
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class HBPEdgeCameraSettings : MonoBehaviour
    {
        [SerializeField] private bool m_EdgesEnabled = true;

        public bool EdgesEnabled
        {
            get { return m_EdgesEnabled; }
            set { m_EdgesEnabled = value; }
        }
    }
}
