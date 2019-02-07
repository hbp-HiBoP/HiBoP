using UnityEngine;
using System.Collections.Generic;

namespace Tools.Unity.Graph
{
    [CreateAssetMenu(fileName = "ShapedCurve", menuName = "Graph/Data/ShapedCurve", order = 1)]
    public class ShapedCurveData : CurveData
    {
        #region Properties
        [SerializeField] float[] m_Shapes;
        public float[] Shapes
        {
            get
            {
                return m_Shapes;
            }
            set
            {
                m_Shapes = value;
            }
        }
        #endregion

        #region Constructor
        public ShapedCurveData(IEnumerable<Vector2> points, IEnumerable<float> shapes, Color color , float width = 3.0f) : base(points, color, width)
        {
            float[] shapeArray = shapes as float[];
            if(shapeArray.Length == Points.Length)
            {
                Shapes = shapeArray;
            }
            else
            {
                Debug.LogWarning("Wrong shape array lenght");
                Shapes = new float[Points.Length];
            }
        }
        #endregion
    }
}