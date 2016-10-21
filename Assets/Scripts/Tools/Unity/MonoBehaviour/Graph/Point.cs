using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Tools.Unity.Graph
{
    public class Point : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        #region Attributs
        [SerializeField]
        Sprite m_Round;
        [SerializeField]
        Sprite m_Cross;
        [SerializeField]
        Sprite m_Square;
        [SerializeField]
        Sprite m_Star;
        [SerializeField]
        Sprite m_Triangle;

        public enum Style
        {
            Cross, Round, Square, Star, Triangle
        }

        #region Parameters
        public Color Color { get { return GetColor(); } set { SetColor(value); } }
        public Style Icon { get { return GetStyle(); } set { SetStyle(value); } }
        public float Size { get { return GetSize(); } set { SetSize(value); } }

        Vector2 m_position;
        public Vector2 Position { get { return m_position; } private set { m_position = value; SetLabel(value); } }
        #endregion

        #endregion

        #region Public Methods
        public void Set(Vector2 position, Color color, Style style, float size)
        {
            Color = color;
            Icon = style;
            Size = size;
            Position = position;
        }
        #endregion

        #region Private Methods
        void SetLabel(Vector2 position)
        {
            float x = position.x;
            float y = position.y;
            x = Mathf.Round(x);
            y = Mathf.Round(y);
            transform.GetChild(0).GetChild(0).GetComponent<Text>().text = "( " + x + " , " + y + " )";
        }
        #endregion

        #region Getter/Setter

        #region Getter
        Color GetColor()
        {
            return GetComponent<Image>().color;
        }

        Style GetStyle()
        {
            Sprite l_sprite = GetComponent<Image>().sprite;
            Style l_style = Style.Cross;
            if (l_sprite == m_Cross) l_style = Style.Cross;
            else if (l_sprite == m_Round) l_style = Style.Round;
            else if (l_sprite == m_Square) l_style = Style.Square;
            else if (l_sprite == m_Star) l_style = Style.Star;
            else if (l_sprite == m_Triangle) l_style = Style.Triangle;
            return l_style;
        }

        float GetSize()
        {
            return GetComponent<RectTransform>().sizeDelta.x;
        }
        #endregion

        #region Setter
        void SetColor(Color color)
        {
            GetComponent<Image>().color = color;
            transform.GetChild(0).GetChild(0).GetComponent<Text>().color = color;
        }

        void SetStyle(Style style)
        {
            Image l_image = GetComponent<Image>();
            switch (style)
            {
                case Style.Cross: l_image.sprite = m_Cross; break;
                case Style.Round: l_image.sprite = m_Round; break;
                case Style.Square: l_image.sprite = m_Square; break;
                case Style.Star: l_image.sprite = m_Star; break;
                case Style.Triangle: l_image.sprite = m_Triangle; break;
            }
        }

        void SetSize(float size)
        {
            GetComponent<RectTransform>().sizeDelta = new Vector2(size, size);
        }
        #endregion

        #endregion

        #region Handlers
        public void OnPointerEnter(PointerEventData eventData)
        {
            transform.parent.SetAsLastSibling();
            transform.GetChild(0).gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            transform.GetChild(0).gameObject.SetActive(false);
        }
        #endregion
    }
}
