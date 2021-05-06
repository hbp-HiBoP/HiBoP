using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Tools.Unity.Components
{
    [RequireComponent(typeof(CanvasScaler))]
    public class CanvasScalerHandler : MonoBehaviour
    {
        #region Properties
        private RectTransform m_RectTransform;
        private CanvasScaler m_CanvasScaler;
        private float m_RawScale
        {
            get
            {
                return (m_CanvasScaler.referenceResolution.x / Screen.width) * (1 - m_CanvasScaler.matchWidthOrHeight) + (m_CanvasScaler.referenceResolution.y / Screen.height) * m_CanvasScaler.matchWidthOrHeight;
            }
        }
        public float Scale
        {
            get
            {
                float scale = m_RawScale;
                return scale > 1 ? 1 : scale;
            }
        }
        #endregion

        #region Private Methods
        private void Awake()
        {
            m_CanvasScaler = GetComponent<CanvasScaler>();
            m_RectTransform = GetComponent<RectTransform>();
        }
        private void Update()
        {
            if (m_RectTransform.hasChanged)
            {
                m_CanvasScaler.enabled = m_RawScale < 1;
                m_RectTransform.hasChanged = false;
            }
        }
        #endregion
    }
}