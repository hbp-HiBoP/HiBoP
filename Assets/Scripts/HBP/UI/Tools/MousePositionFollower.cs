using UnityEngine;

namespace HBP.UI.Tools
{
    [RequireComponent(typeof(RectTransform))]
    public class MousePositionFollower : MonoBehaviour
    {
        #region Properties
        public Vector2 Offset;
        public bool FollowX = true;
        public bool FollowY = true;
        public bool AlwaysUpdate = true;

        RectTransform m_RectTransform;
        bool m_Initialized;
        #endregion

        #region Public Methods
        public void Follow()
        {
            if (!m_Initialized) Initialize();
            Vector3 position = Input.mousePosition + (Vector3)Offset;
            if(!FollowX)
            {
                position.x = m_RectTransform.position.x; 
            }
            if(!FollowY)
            {
                position.y = m_RectTransform.position.y;
            }
            m_RectTransform.position = position;
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