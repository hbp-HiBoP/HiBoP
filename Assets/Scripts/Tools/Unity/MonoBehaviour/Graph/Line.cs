using UnityEngine;
using UnityEngine.UI;

namespace Tools.Unity.Graph
{
    public class Line : MonoBehaviour
    {
        #region Parameters
        float m_width;
        public float Width
        {
            set
            {
                SetWidth(value);
            }
            get
            {
                return m_width;
            }
        }
        public Color Color { get { return GetColor(); } set { SetColor(value); } }
        public Point point1;
        public Point point2;
        #endregion

        #region Public methods
        public void Set(float width, Point p1, Point p2, Color color)
        {
            point1 = p1;
            point2 = p2;
            Width = width;
            SetSize(width, p1.transform.localPosition, p2.transform.localPosition);
            SetPosition(p1.transform.localPosition, p2.transform.localPosition);
            SetColor(color);
        }

        public void UpdateLine()
        {
            SetSize(Width, point1.transform.localPosition, point2.transform.localPosition);
            SetPosition(point1.transform.localPosition, point2.transform.localPosition);
        }
        #endregion

        #region Private methods
        void SetPosition(Vector2 p1, Vector2 p2)
        {
            RectTransform l_rect = GetComponent<RectTransform>();
            float l_degree = Mathf.Rad2Deg * Mathf.Atan2(p2.y - p1.y, p2.x - p1.x);
            l_rect.localPosition = p1;
            l_rect.localEulerAngles = new Vector3(0, 0, l_degree - 90);
        }

        void SetSize(float width, Vector2 p1, Vector2 p2)
        {
            RectTransform l_rect = GetComponent<RectTransform>();
            float l_height = Mathf.Sqrt(Mathf.Pow(p2.x - p1.x, 2) + Mathf.Pow(p2.y - p1.y, 2));
            l_rect.sizeDelta = new Vector2(width, l_height);
        }
        #endregion

        #region Getter/Setter
        void SetWidth(float width)
        {
            m_width = width;
            RectTransform l_rect = GetComponent<RectTransform>();
            l_rect.sizeDelta = new Vector2(width, l_rect.sizeDelta.y);
        }

        Color GetColor()
        {
            return GetComponent<Image>().color;
        }

        void SetColor(Color color)
        {
            GetComponent<Image>().color = color;
        }
        #endregion
    }
}
