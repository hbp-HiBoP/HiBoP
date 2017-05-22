using UnityEngine;
using UnityStandardAssets.ImageEffects;

namespace HBP.Module3D
{
    public class View3D : MonoBehaviour
    {
        #region Properties
        /// <summary>
        /// Camera associated to the view
        /// </summary>
        private Camera3D m_Camera;

        private bool m_IsFocused = false;
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

        private bool m_IsClicked = false;
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

        private bool m_IsMinimized = false;
        /// <summary>
        /// True if the view is minimized
        /// </summary>
        public bool IsMinimized
        {
            get
            {
                return m_IsMinimized;
            }
            set
            {
                m_IsMinimized = value;
                if (!m_IsMinimized)
                {
                    m_Camera.CullingMask = m_RegularCullingMask;
                }
                else
                {
                    m_Camera.CullingMask = m_MinimizedCullingMask;
                }
                m_Camera.GetComponent<Camera>().enabled = !m_IsMinimized;
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
                return m_Camera.AutomaticRotation;
            }
            set
            {
                m_Camera.AutomaticRotation = value;
            }
        }

        /// <summary>
        /// Set the edge mode
        /// </summary>
        public bool EdgesMode
        {
            get
            {
                return m_Camera.GetComponent<EdgeDetection>().enabled;
            }
            set
            {
                m_Camera.GetComponent<EdgeDetection>().enabled = value;
            }
        }

        /// <summary>
        /// Layer of the view
        /// </summary>
        public string Layer { get; set; }

        /// <summary>
        /// Set the texture on which the camera renders
        /// </summary>
        /// <param name="texture">Texture to be rendered on</param>
        public RenderTexture TargetTexture
        {
            get
            {
                return m_Camera.GetComponent<Camera>().targetTexture;
            }
            set
            {
                m_Camera.GetComponent<Camera>().targetTexture = value;
            }
        }

        /// <summary>
        /// Aspect ration of the camera
        /// </summary>
        public float Aspect
        {
            get
            {
                return m_Camera.GetComponent<Camera>().aspect;
            }
            set
            {
                m_Camera.GetComponent<Camera>().aspect = value;
            }
        }

        /// <summary>
        /// Default minimized culling mask value
        /// </summary>
        private int m_MinimizedCullingMask;
        /// <summary>
        /// Default regular culling mask value
        /// </summary>
        private int m_RegularCullingMask;
        #endregion

        #region Private Methods
        private void Awake()
        {
            m_Camera = transform.GetComponentInChildren<Camera3D>();
        }
        private void Start()
        {
            int layer = 0;
            layer |= 1 << LayerMask.NameToLayer(Layer);
            switch (GetComponentInParent<Base3DScene>().Type)
            {
                case SceneType.SinglePatient:
                    layer |= 1 << LayerMask.NameToLayer("Meshes_SP");
                    break;
                case SceneType.MultiPatients:
                    layer |= 1 << LayerMask.NameToLayer("Meshes_MP");
                    break;
                default:
                    break;
            }
            m_RegularCullingMask = layer;
            m_MinimizedCullingMask = 0;

            if (!m_IsMinimized)
            {
                m_Camera.CullingMask = m_RegularCullingMask;
            }
            else
            {
                m_Camera.CullingMask = m_MinimizedCullingMask;
            }
        }
        private void Update()
        {
            m_Camera.GetComponent<Camera>().backgroundColor = IsClicked ? m_ClickedColor : (IsFocused ? m_FocusedColor : m_RegularColor);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Synchronize the camera of this view using the camera from a reference view
        /// </summary>
        /// <param name="reference"></param>
        public void SynchronizeCamera(View3D reference)
        {
            m_Camera.transform.position = reference.m_Camera.transform.position;
            m_Camera.transform.rotation = reference.m_Camera.transform.rotation;
            m_Camera.Target = reference.m_Camera.Target;
        }
        /// <summary>
        /// Set the viewport of the camera
        /// </summary>
        /// <param name="viewport">Viewport</param>
        public void SetViewport(float x, float y, float width, float height)
        {
            m_Camera.GetComponent<Camera>().rect = new Rect(x / Screen.width, y / Screen.height, width / Screen.width, height / Screen.height);
        }
        #endregion
    }
}