using HBP.Module3D;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HBP.UI.Module3D
{
    public class SelectionRing : MonoBehaviour
    {
        #region Properties
        Site m_Site;
        public Site Site
        {
            get
            {
                return m_Site;
            }
            set
            {
                m_Site = value;
                m_Image.enabled = value != null;
            }
        }
        public Camera ViewCamera { get; set; }
        public RectTransform Viewport { get; set; }
        RectTransform m_RectTransform;
        public float MinSize;
        public float RatioOffset;
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
        void Display()
        {
            if(m_Site != null)
            {
                Vector3 centerPosition = m_Site.transform.position;
                Vector3 borderPosition = centerPosition + m_Site.transform.localScale.x * ViewCamera.transform.right;
                Vector3 centerScreenPosition = ViewCamera.WorldToScreenPoint(centerPosition);
                Vector3 borderScreenPosition = ViewCamera.WorldToScreenPoint(borderPosition);
                float size = 2 * (borderScreenPosition.x - centerScreenPosition.x);
                size += size * RatioOffset;
                size = Mathf.Max(size, MinSize);
                m_RectTransform.localPosition = centerScreenPosition + (Vector3)Viewport.rect.min;
                m_RectTransform.sizeDelta = new Vector2(size, size);
            }
        }
        #endregion
    }
}