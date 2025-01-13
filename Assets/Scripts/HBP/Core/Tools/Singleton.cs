using UnityEngine;

namespace HBP.Core.Tools
{
    public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        #region Properties
        protected static T m_Instance;
        #endregion

        #region Private Methods
        private void Awake()
        {
            if (m_Instance != null && m_Instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                m_Instance = this as T;
                Initialization();
            }
        }
        #endregion

        #region Public Methods
        protected virtual void Initialization()
        {
        }
        #endregion
    }
}