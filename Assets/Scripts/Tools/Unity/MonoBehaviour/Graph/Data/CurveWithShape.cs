using UnityEngine;

namespace Tools.Unity.Graph.Data
{
    public class CurveWithShape : Curve
    {
        #region Properties
        protected float[] shapes;
        public float[] Shapes { get { return shapes; } set { shapes = value;} }

        protected float shapesTransparency;
        public float ShapesTransparency { get { return shapesTransparency; } set { shapesTransparency = value; } }
        #endregion

        #region Constructor
        public CurveWithShape(string label, float width, Color color, Vector2[] points, float[] shapes, Point.Style style, bool connectingPoints) : base(label,width,color,points,style,connectingPoints)
        {
            Shapes = shapes;
        }
        public CurveWithShape() : this("", 0, Color.black, new Vector2[0], new float[0], Point.Style.Round, true)
        {
        }
        #endregion
    }
}