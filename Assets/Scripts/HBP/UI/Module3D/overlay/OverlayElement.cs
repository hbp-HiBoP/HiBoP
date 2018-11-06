using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HBP.UI.Module3D
{
    public abstract class OverlayElement : MonoBehaviour
    {
        #region Properties
        protected RectTransform m_RectTransform;
        protected float m_InitialAnchoredX;
        protected float m_InitialAnchoredY;
        #endregion

        #region Public Methods
        public void Initialize()
        {
            m_RectTransform = GetComponent<RectTransform>();
            m_InitialAnchoredX = m_RectTransform.anchoredPosition.x;
            m_InitialAnchoredY = m_RectTransform.anchoredPosition.y;
        }
        public void SetVerticalOffset(float offset)
        {
            m_RectTransform.anchoredPosition = new Vector2(m_RectTransform.anchoredPosition.x, m_InitialAnchoredY + offset);
        }
        public void SetHorizontalOffset(float offset)
        {
            m_RectTransform.anchoredPosition = new Vector2(m_InitialAnchoredX + offset, m_RectTransform.anchoredPosition.y);
        }
        #endregion
    }
}