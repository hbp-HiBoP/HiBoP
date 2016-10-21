using UnityEngine;
using g = Tools.Unity.Graph;

namespace Tools.Unity.Graph.Data
{
    public class Curve
    {
        #region Properties
        protected string m_label ;
        public string Label { get { return m_label; } set { m_label = value; } }

        protected float m_width;
        public float Width { get { return m_width; } set { m_width = value; } }

        protected Color m_color;
        public Color Color { get { return m_color; } set { m_color = value; } }

        protected g.Point.Style m_style;
        public g.Point.Style Style { get { return m_style; } set { m_style = value; } }

        protected bool m_connectingPoints;
        public bool ConnectingPoints { get { return m_connectingPoints; } set { m_connectingPoints = value; } }

        protected Vector2[] m_points;
        public Vector2[] Points { get { return m_points; } set { m_points = value; } }
        #endregion

        #region Constructor
        public Curve(string label, float width, Color color, Vector2[] points,g.Point.Style style,bool connectingPoints)
        {
            Label = label;
            Width = width;
            Color = color;
            Points = points;
            Style = style;
            ConnectingPoints = connectingPoints;
        }
        #endregion
    }
}