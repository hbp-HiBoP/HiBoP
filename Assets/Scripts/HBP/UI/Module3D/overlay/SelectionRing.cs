using HBP.Module3D;
using UnityEngine;

namespace HBP.UI.Module3D
{
    /// <summary>
    /// Overlay element of a view to show the currently selected site
    /// </summary>
    public class SelectionRing : MonoBehaviour
    {
        #region Properties
        /// <summary>
        /// Currently selected site
        /// </summary>
        public Site Site { get; set; }
        /// <summary>
        /// Camera of the corresponding View3D
        /// </summary>
        public Camera ViewCamera { get; set; }
        /// <summary>
        /// Reference to the viewport of the corresponding View3DUI
        /// </summary>
        public RectTransform Viewport { get; set; }
        /// <summary>
        /// RectTransform of this object
        /// </summary>
        RectTransform m_RectTransform;
        /// <summary>
        /// Minimum size of the selection ring
        /// </summary>
        private float m_MinSize = 20;
        /// <summary>
        /// Offset required so the selection ring is not too close to the selected site
        /// </summary>
        private float m_RatioOffset = 0.7f;
        /// <summary>
        /// Image of the selection ring
        /// </summary>
        [SerializeField] UnityEngine.UI.Image m_Image;
        #endregion

        #region Private Methods
        void Awake()
        {
            m_RectTransform = transform as RectTransform;
            Display();
        }
        void Update()
        {
            Display();
        }
        /// <summary>
        /// Displays the selection ring if a site is selected and active
        /// </summary>
        void Display()
        {
            if (Site == null)
            {
                m_Image.enabled = false;
            }
            else if (!Site.IsActive)
            {
                m_Image.enabled = false;
            }
            else
            {
                m_Image.enabled = true;
                Vector3 centerPosition = Site.transform.position;
                Vector3 borderPosition = centerPosition + Site.transform.localScale.x * ViewCamera.transform.right;
                Vector3 centerScreenPosition = ViewCamera.WorldToScreenPoint(centerPosition);
                Vector3 borderScreenPosition = ViewCamera.WorldToScreenPoint(borderPosition);
                float size = 2 * (borderScreenPosition.x - centerScreenPosition.x);
                size += size * m_RatioOffset;
                size = Mathf.Max(size, m_MinSize);
                m_RectTransform.localPosition = centerScreenPosition + (Vector3)Viewport.rect.min;
                m_RectTransform.sizeDelta = new Vector2(size, size);
            }
        }
        #endregion
    }
}