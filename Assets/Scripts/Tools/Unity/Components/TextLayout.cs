using UnityEngine;
using UnityEngine.UI;

namespace Tools.Unity
{
    [RequireComponent(typeof(RectTransform))]
    public class TextLayout : MonoBehaviour
    {
        #region Properties
        Image m_image;
        Text m_text;
        RectTransform m_rect;
        
        public float Dif { get { return (m_rect.rect.width - (m_image.rectTransform.rect.width + m_text.preferredWidth)); } }
        #endregion

        #region Public Methods
        public void DisplayLabel(bool display)
        {
            if(display)
            {
                m_text.enabled = true;
                m_image.rectTransform.anchorMin = new Vector2(1f, 0f);
                m_image.rectTransform.anchorMax = new Vector2(1f, 1f);
                m_image.rectTransform.pivot = new Vector2(1f, 0.5f);
            }
            else
            {
                m_text.enabled = false;
                m_image.rectTransform.anchorMin = new Vector2(0.5f, 0);
                m_image.rectTransform.anchorMax = new Vector2(0.5f, 1);
                m_image.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            }
        }
        #endregion

        #region Private Methods
        void Awake()
        {
            m_rect = GetComponent<RectTransform>();
            m_image = transform.GetChild(1).GetComponent<Image>();
            m_text = GetComponentInChildren<Text>();
        }
        #endregion
    }
}

