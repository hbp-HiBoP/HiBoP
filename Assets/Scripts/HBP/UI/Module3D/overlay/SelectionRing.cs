using HBP.Module3D;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HBP.UI.Module3D
{
    public class SelectionRing : MonoBehaviour
    {
        #region Properties
        public Site Site { get; set; }
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
                size += size * RatioOffset;
                size = Mathf.Max(size, MinSize);
                m_RectTransform.localPosition = centerScreenPosition + (Vector3)Viewport.rect.min;
                m_RectTransform.sizeDelta = new Vector2(size, size);
            }
        }
        #endregion
    }
}