using UnityEngine;

namespace HBP.Module3D
{
    public class View : MonoBehaviour
    {
        #region Properties
        public Camera3D Camera { get; set; }
        private bool m_IsFocused;
        public bool IsFocused
        {
            get
            {
                return m_IsFocused;
            }
        }
        private bool m_IsMinimized;
        public bool IsMinimized
        {
            get
            {
                return m_IsMinimized;
            }
        }
        public int LineID { get; set; }
        #endregion

        #region Private Methods
        private void Awake()
        {
            Camera = transform.GetComponentInChildren<Camera3D>();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Synchronize the camera of this view using the camera from a reference view
        /// </summary>
        /// <param name="reference"></param>
        public void SynchronizeCamera(View reference)
        {
            Camera.transform.position = reference.Camera.transform.position;
            Camera.transform.rotation = reference.Camera.transform.rotation;
            Camera.Target = reference.Camera.Target;
        }
        #endregion
    }
}