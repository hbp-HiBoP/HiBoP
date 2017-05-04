using UnityEngine;

namespace HBP.Module3D
{
    public class View : MonoBehaviour
    {
        #region Properties
        /// <summary>
        /// Camera associated to the view
        /// </summary>
        public Camera3D Camera { get; set; }

        private bool m_IsFocused;
        /// <summary>
        /// True if this view is the last view in which the user clicked
        /// </summary>
        public bool IsFocused
        {
            get
            {
                return m_IsFocused;
            }
        }

        private bool m_IsClicked;
        /// <summary>
        /// True if this view is the last object in which the user clicked
        /// </summary>
        public bool IsClicked
        {
            get
            {
                return m_IsClicked;
            }
        }

        private bool m_IsMinimized;
        /// <summary>
        /// True if the view is minimized
        /// </summary>
        public bool IsMinimized
        {
            get
            {
                return m_IsMinimized;
            }
        }

        /// <summary>
        /// Line in which the view belongs
        /// </summary>
        public int LineID { get; set; }

        protected Color m_ClickedColor = new Color(0.45f, 0.48f, 0.58f);
        /// <summary>
        /// Color of the background when the view is clicked
        /// </summary>
        public Color ClickedColor
        {
            get { return m_ClickedColor; }
            set { m_ClickedColor = value; }
        }

        protected Color m_FocusedColor = new Color(0.35f, 0.38f, 0.48f);
        /// <summary>
        /// Color of the background when the view is focused
        /// </summary>
        public Color FocusedColor
        {
            get { return m_FocusedColor; }
            set { m_FocusedColor = value; }
        }
        
        protected Color m_RegularColor = new Color(0.65f, 0.65f, 0.65f);
        /// <summary>
        /// Color of the background when the view is not focused
        /// </summary>
        public Color RegularColor
        {
            get { return m_RegularColor; }
            set { m_RegularColor = value; }
        }

        /// <summary>
        /// Start or stop the automatic rotation of the camera of this view
        /// </summary>
        public bool AutomaticRotation
        {
            get
            {
                return Camera.AutomaticRotation;
            }
            set
            {
                Camera.AutomaticRotation = value;
            }
        }

        /// <summary>
        /// Layer of the view
        /// </summary>
        public string Layer { get; set; }

        // Default Culling Masks values
        private int m_MinimizedCullingMask;
        private int m_RegularCullingMask;
        #endregion

        #region Private Methods
        private void Awake()
        {
            Camera = transform.GetComponentInChildren<Camera3D>();
        }
        private void Start()
        {
            int layer = 0;
            layer |= 1 << LayerMask.NameToLayer(Layer);
            m_RegularCullingMask = layer;
            m_MinimizedCullingMask = 0;

            if (!m_IsMinimized)
            {
                Camera.CullingMask = m_RegularCullingMask;
            }
            else
            {
                Camera.CullingMask = m_MinimizedCullingMask;
            }
        }
        private void Update()
        {
            Camera.GetComponent<Camera>().backgroundColor = IsClicked ? m_ClickedColor : (IsFocused ? m_FocusedColor : m_RegularColor);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Set the minimized state of the view
        /// </summary>
        /// <param name="minimized"></param>
        public void SetMinimized(bool minimized)
        {
            m_IsMinimized = minimized;
            if (m_IsMinimized)
            {
                Camera.CullingMask = m_RegularCullingMask;
            }
            else
            {
                Camera.CullingMask = m_MinimizedCullingMask;
            }
        }
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