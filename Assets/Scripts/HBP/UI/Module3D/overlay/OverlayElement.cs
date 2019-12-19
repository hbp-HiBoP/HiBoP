using UnityEngine;

namespace HBP.UI.Module3D
{
    /// <summary>
    /// Base class of any overlay element on a scene, column or view
    /// </summary>
    public abstract class OverlayElement : MonoBehaviour
    {
        #region Properties
        /// <summary>
        /// RectTransform of this game object
        /// </summary>
        protected RectTransform m_RectTransform;
        /// <summary>
        /// Initial X anchored position of this object
        /// </summary>
        protected float m_InitialAnchoredX;
        /// <summary>
        /// Initial Y anchored position of this object
        /// </summary>
        protected float m_InitialAnchoredY;
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize the overlay element with basic information
        /// </summary>
        public void Initialize()
        {
            m_RectTransform = GetComponent<RectTransform>();
            m_InitialAnchoredX = m_RectTransform.anchoredPosition.x;
            m_InitialAnchoredY = m_RectTransform.anchoredPosition.y;
        }
        /// <summary>
        /// Set a vertical offset to this overlay element (used when columns or views are minimized so the overlay element stays on a non-minimized column or view)
        /// </summary>
        /// <param name="offset">Vertical offset</param>
        public void SetVerticalOffset(float offset)
        {
            m_RectTransform.anchoredPosition = new Vector2(m_RectTransform.anchoredPosition.x, m_InitialAnchoredY + offset);
        }
        /// <summary>
        /// Set a horizontal offset to this overlay element (used when columns or views are minimized so the overlay element stays on a non-minimized column or view)
        /// </summary>
        /// <param name="offset">Horizontal offset</param>
        public void SetHorizontalOffset(float offset)
        {
            m_RectTransform.anchoredPosition = new Vector2(m_InitialAnchoredX + offset, m_RectTransform.anchoredPosition.y);
        }
        #endregion
    }
}