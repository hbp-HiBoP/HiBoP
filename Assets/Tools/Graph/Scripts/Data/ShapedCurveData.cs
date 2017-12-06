using UnityEngine;
using System.Collections.Generic;

namespace Tools.Unity.Graph
{
    public class ShapedCurveData : CurveData
    {
        #region Properties
        public float[] Shapes { get; set; }
        #endregion

        #region Constructor
        public ShapedCurveData(string label, string id, IEnumerable<Vector2> points, IEnumerable<float> shapes, Color color , float width = 3.0f) : base(label, id, points, color, width)
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