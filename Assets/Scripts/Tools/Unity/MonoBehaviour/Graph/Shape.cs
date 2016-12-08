using UnityEngine;
using UnityEngine.UI;

namespace Tools.Unity.Graph
{
    public class Shape : MonoBehaviour
    {
        #region Parameters
        public Color Color { get { return GetComponent<RawImage>().color; } set { GetComponent<RawImage>().color = new Color(value.r,value.g,value.b,0.1f); } }

        Point m_point1;
        public Point Point1 { get { return m_point1; } private set { m_point1 = value; } }
        float m_standardDeviation1;

        Point m_point2;
        public Point Point2 { get { return m_point2; } private set { m_point2 = value; } }
        float m_standardDeviation2;

        int m_width = 8;
        Vector2 m_ratio;
        float m_pivot;
        Texture2D m_texture;
        #endregion

        #region Public methods
        public void Set(Point point1, Point point2,float sd1, float sd2, Vector2 ratio, Color color)
        {
            Point1 = point1;
            Point2 = point2;
            Color = color;
            m_standardDeviation1 = sd1;
            m_standardDeviation2 = sd2;
            UpdateShape(ratio);
        }

        public void UpdateShape(Vector2 ratio)
        {
            m_ratio = ratio;
            GenerateTexture();
            SetRectTransform(Point1.transform.localPosition, Point2.transform.localPosition, m_pivot);
        }
        #endregion

        #region Private methods
        void SetRectTransform(Vector2 p1, Vector2 p2,float pivot)
        {
            RectTransform l_rect = GetComponent<RectTransform>();
            float width = Mathf.Abs(p2.x - p1.x);
            float height = ((float)m_texture.height / m_texture.width) * width;
            l_rect.sizeDelta = new Vector2(width, height);
            l_rect.localPosition = p1;
            l_rect.localEulerAngles = new Vector3(0, 0, 0);
            l_rect.pivot = new Vector2(0, pivot);
        }

        void GenerateTexture()
        {
            // Width.
            float sd1 = m_standardDeviation1 * m_ratio.y;
            float sd2 = m_standardDeviation2 * m_ratio.y;
            Vector2 point1 = Point1.transform.localPosition;
            Vector2 point2 = Point2.transform.localPosition;

            // Detect which point is TOP/BOT/LEFT/RIGHT side.
            Vector2 bl, br, tl, tr;
            float xMin = float.MaxValue;
            float xMax = float.MinValue;
            if (Point1.transform.localPosition.x > Point2.transform.localPosition.x)
            {
                bl = new Vector3(point2.x, point2.y - sd2 / 2);
                tl = new Vector2(point2.x, point2.y + sd2 / 2);
                br = new Vector2(point1.x, point1.y - sd1 / 2);
                tr = new Vector2(point1.x, point1.y + sd1 / 2);
                xMin = point2.x;
                xMax = point1.x;
            }
            else
            {
                bl = new Vector2(point1.x, point1.y - sd1 / 2);
                tl = new Vector2(point1.x, point1.y + sd1 / 2);
                br = new Vector3(point2.x, point2.y - sd2 / 2);
                tr = new Vector2(point2.x, point2.y + sd2 / 2);
                xMin = point1.x;
                xMax = point2.x;
            }

            //Calculate Limits.
            float yMax = float.MinValue;
            if (tl.y > tr.y)
            {
                yMax = tl.y;
            }
            else
            {
                yMax = tr.y;
            }
            float yMin = float.MaxValue;
            if (bl.y > br.y)
            {
                yMin = br.y;
            }
            else
            {
                yMin = bl.y;
            }
            // Calculate X,Y range.        
            float xDif = xMax - xMin;
            float yDif = yMax - yMin;
            m_pivot = (point1.y - yMin) / yDif;

            // Calculate Width/Height of the texture.
            int height = Mathf.RoundToInt(m_width * (yDif / xDif));

            // Calculate X ratio and Y ratio.
            float xR = xDif / m_width;
            float yR = yDif / height;

            // Calculate line equations.
            float aTop = (tr.y - tl.y) / (tr.x - tl.x);
            float aBot = (br.y - bl.y) / (br.x - bl.x);
            float bTop = tr.y - aTop * tr.x;
            float bBot = br.y - aBot * br.x;

            // Generate texture.
            Texture2D l_texture = new Texture2D(m_width, height);
            l_texture.wrapMode = TextureWrapMode.Clamp;

            // Set the texture.
            for (int x = 0; x < m_width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    l_texture.SetPixel(x, y, new Color(0, 0, 0, 0));
                }
                float X = x * xR + bl.x;
                for (int y = 0; y < height; y++)
                {
                    float Y = y * yR + yMin;
                    if ((Y > aTop * X + bTop) || Y < aBot * X + bBot)
                    {
                        l_texture.SetPixel(x, y, new Color(0, 0, 0, 0));
                    }
                    else
                    {
                        l_texture.SetPixel(x, y, new Color(1, 1, 1, 1));
                    }
                }
            }

            // Apply change.
            l_texture.Apply();
            m_texture = l_texture;
            GetComponent<RawImage>().texture = l_texture;
        }
        #endregion
    }
}
