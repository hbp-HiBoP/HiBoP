using UnityEngine;
using d=Tools.Unity.Graph.Data;

namespace Tools.Unity.Graph
{
    public class Curve : MonoBehaviour
    {
        #region Properties
        protected d.Curve m_dataCurve;
        public d.Curve DataCurve { get { return m_dataCurve; } private set { m_dataCurve = value;   } }

        protected Vector2 m_ratio;
        public Vector2 Ratio { get { return m_ratio; } private set { m_ratio = value; } }

        protected Vector2 m_origin;
        public Vector2 Origin { get { return m_origin; } private set { m_origin = value; } }

        [SerializeField]
        protected GameObject m_Point;
        [SerializeField]
        protected GameObject m_Line;

        protected Point[] m_points = new Point[0];
        protected RectTransform pointsRect;
        protected Line[] m_lines = new Line[0];
        protected RectTransform linesRect;
        #endregion

        #region Public Methods
        public virtual void Set(d.Curve dataCurve, Vector2 origin, Vector2 ratio)
        {
            Clear();
            DataCurve = dataCurve;

            Origin = origin;
            Ratio = ratio;
            Plot();
        }

        public virtual void Move(Vector2 origin)
        {
            Vector2 oldOrigin = Origin;
            Origin = origin;
            Vector3 move = oldOrigin - origin;
            move = new Vector3(move.x * Ratio.x, move.y * Ratio.y, 0);
            GetComponent<RectTransform>().localPosition += move;
        }

        public virtual void Zoom(Vector2 ratio)
        {
            if (ratio == Ratio)
            {
                return;
            }
            Ratio = ratio;
            GetComponent<RectTransform>().offsetMin = new Vector3(0, 0);
            GetComponent<RectTransform>().offsetMax = new Vector3(0, 0);
            for (int i = 0; i < m_points.Length; i++)
            {
                UpdatePoint(i);
            }
            for (int i = 0; i < m_lines.Length; i++)
            {
                UpdateLine(i);
            }
        }
        #endregion

        #region Private Methods
        void Awake()
        {
            SetRects();
        }

        protected virtual void SetRects()
        {
            pointsRect = transform.GetChild(0).GetComponent<RectTransform>();
            linesRect = transform.GetChild(1).GetComponent<RectTransform>();
        }

        protected virtual void Plot()
        {
            m_points = new Point[m_dataCurve.Points.Length];
            m_lines = new Line[m_dataCurve.Points.Length - 1];
            for (int i = 0; i < m_points.Length; i++)
            {
                AddPoint(i);
            }
            for (int i = 0; i < m_lines.Length; i++)
            {
                AddLine(i);
            }
        }

        protected virtual void Clear()
        {
            foreach(Point point in m_points)
            {
                Destroy(point.gameObject);
            }
            foreach(Line line in m_lines)
            {
                Destroy(line.gameObject);
            }

            m_points = new Point[0];
            m_lines = new Line[0];
            m_dataCurve = null;
            m_ratio = new Vector2();
            m_origin = new Vector2();
        }

        protected virtual void AddPoint(int i)
        {
            GameObject l_gameObject = Instantiate(m_Point) as GameObject;
            Point l_point = l_gameObject.GetComponent<Point>();
            l_point.transform.SetParent(pointsRect);
            Vector2 position = DataCurve.Points[i] - Origin;
            position = new Vector2(position.x * Ratio.x, position.y * Ratio.y);
            l_point.transform.localPosition = position;
            Point l_pointToAdd = l_point.GetComponent<Point>();
            l_pointToAdd.Set(DataCurve.Points[i], DataCurve.Color, DataCurve.Style, DataCurve.Width);
            m_points[i] = l_pointToAdd;
        }

        protected virtual void UpdatePoint(int i)
        {
            Vector2 position = m_points[i].Position - Origin;
            position = new Vector3(position.x * Ratio.x, position.y * Ratio.y, 0);
            m_points[i].transform.localPosition = position;
        }

        protected virtual void AddLine(int i)
        {
            GameObject l_object = Instantiate(m_Line);
            l_object.transform.SetParent(linesRect);
            Line l_line = l_object.GetComponent<Line>();
            m_lines[i] = l_line;
            l_line.Set(DataCurve.Width, m_points[i], m_points[i + 1], DataCurve.Color);
        }

        protected virtual void UpdateLine(int i)
        {
            m_lines[i].UpdateLine();
        }

        protected virtual void SetColor()
        {
            foreach (Transform point in pointsRect)
            {
                point.GetComponent<Point>().Color = DataCurve.Color;
            }
            foreach (Transform line in linesRect)
            {
                line.GetComponent<Line>().Color = DataCurve.Color;
            }
        }

        protected virtual void SetStyle()
        {
            foreach (Transform point in pointsRect)
            {
                point.GetComponent<Point>().Icon = DataCurve.Style;
            }
        }

        protected virtual void SetSize()
        {
            foreach (Transform point in pointsRect)
            {
                point.GetComponent<Point>().Size = DataCurve.Width;
            }
            foreach(Transform line in linesRect)
            {
                line.GetComponent<Line>().Width = DataCurve.Width;
            }
        }
        #endregion
    }
}