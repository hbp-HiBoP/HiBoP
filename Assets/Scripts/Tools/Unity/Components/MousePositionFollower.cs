using UnityEngine;

namespace Tools.Unity.Components
{
    [RequireComponent(typeof(RectTransform))]
    public class MousePositionFollower : MonoBehaviour
    {
        #region Properties
        public Vector2 Offset;
        public bool AlwaysUpdate;

        RectTransform m_RectTransform;
        bool m_Initialized;
        #endregion

        #region Public Methods
        public void Follow()
        {
            if (!m_Initialized) Initialize();
            m_RectTransform.position = Input.mousePosition + (Vector3)Offset;
        }
        #endregion

        #region Private Methods
        void Update()
        {
            if(AlwaysUpdate) Follow();
        }
        void Initialize()
        {
            m_RectTransform = GetComponent<RectTransform>();
            m_Initialized = true;
        }
        #endregion
    }
}