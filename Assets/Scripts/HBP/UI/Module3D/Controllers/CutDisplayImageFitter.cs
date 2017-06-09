using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D
{
    [ExecuteInEditMode]
    public class CutDisplayImageFitter : MonoBehaviour
    {
        private LayoutElement m_LayoutElement;
        private RectTransform m_RectTransform;

        private void Awake()
        {
            m_LayoutElement = GetComponent<LayoutElement>();
            m_RectTransform = GetComponent<RectTransform>();
        }

        public void OnRectTransformDimensionsChange()
        {
            if (!m_LayoutElement || !m_RectTransform) return;

            m_LayoutElement.preferredHeight = m_RectTransform.rect.width;
        }
    }
}