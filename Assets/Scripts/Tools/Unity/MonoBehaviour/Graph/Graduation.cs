using UnityEngine;
using UnityEngine.UI;

namespace Tools.Unity.Graph
{
    public class Graduation : MonoBehaviour
    {
        #region Attributs

        [SerializeField]
        RectTransform m_labelRect;
        [SerializeField]
        RectTransform m_imageRect;

        #region Parameters
        public float Height = 12.0f;
        public float Width = 2.0f;

        Color m_color;
        public Color Color { get { return m_color; } set { SetColor(value); } }
        #endregion

        #endregion

        #region Public Methods

        public void Set(string label, int fontSize, Color fontColor, float position, Axe.SideEnum side)
        {
            SetPosition(position, side);
            SetImage(side);
            SetLabel(label, fontSize, side);
            Color = fontColor;
        }

        #endregion

        #region Private Methods

        void SetLabel(string label, int size, Axe.SideEnum side)
        {
            Text l_label = m_labelRect.GetComponent<Text>();
            l_label.text = label;
            l_label.fontSize = size;
            switch (side)
            {
                case Axe.SideEnum.absciss: m_labelRect.offsetMax = new Vector2(0, -Height / 2); break;
                case Axe.SideEnum.ordinate: m_labelRect.offsetMax = new Vector2(-Height / 2, 0); break;
            }
        }

        void SetPosition(float position, Axe.SideEnum side)
        {
            RectTransform l_rect = GetComponent<RectTransform>();
            switch (side)
            {
                case Axe.SideEnum.absciss:
                    l_rect.anchorMin = new Vector2(0, 0);
                    l_rect.anchorMax = new Vector2(0, 1);
                    l_rect.pivot = new Vector2(0.5f, 1f);
                    l_rect.sizeDelta = new Vector2(l_rect.parent.GetComponent<RectTransform>().rect.width / 11.0f, 0);
                    l_rect.localPosition = position * Vector3.right;
                    break;

                case Axe.SideEnum.ordinate:
                    l_rect.anchorMin = new Vector2(0, 0);
                    l_rect.anchorMax = new Vector2(1, 0);
                    l_rect.pivot = new Vector2(1f, 0.5f);
                    l_rect.sizeDelta = new Vector2(0, l_rect.parent.GetComponent<RectTransform>().rect.height / 11.0f);
                    l_rect.localPosition = position * Vector3.up;
                    break;
            }
        }

        void SetImage(Axe.SideEnum side)
        {
            switch (side)
            {
                case Axe.SideEnum.absciss:
                    m_imageRect.anchorMin = new Vector2(0.5f, 1f);
                    m_imageRect.anchorMax = new Vector2(0.5f, 1f);
                    m_imageRect.pivot = new Vector2(0.5f, 0.5f);
                    m_imageRect.sizeDelta = new Vector2(Width, Height);
                    m_imageRect.localPosition = Vector3.zero;
                    break;

                case Axe.SideEnum.ordinate:
                    m_imageRect.anchorMin = new Vector2(1f, 0.5f);
                    m_imageRect.anchorMax = new Vector2(1f, 0.5f);
                    m_imageRect.pivot = new Vector2(0.5f, 0.5f);
                    m_imageRect.sizeDelta = new Vector2(Height, Width);
                    m_imageRect.localPosition = Vector3.zero;
                    break;
            }
        }

        #region Getter/Setter
        void SetColor(Color color)
        {
            m_color = color;
            m_imageRect.GetComponent<Image>().color = color;
            m_labelRect.GetComponent<Text>().color = color;
        }
        #endregion

        #endregion
    }
}
