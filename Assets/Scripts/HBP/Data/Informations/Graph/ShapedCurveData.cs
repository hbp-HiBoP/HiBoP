using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI.Extensions;

namespace HBP.Data.Informations.Graphs
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
                SetPropertyUtility.SetClass(ref m_Shapes, value);
            }
        }
        #endregion

        #region Public Methods
        public virtual void Init(IEnumerable<Vector2> points, IEnumerable<float> shapes, Color color, float width)
        {
            base.Init(points, color, width);
            float[] shapeArray = shapes as float[];
            if (shapeArray.Length == Points.Length)
            {
                Shapes = shapeArray;
            }
            else
            {
                Debug.LogWarning("Wrong shape array length");
                Shapes = new float[Points.Length];
            }
        }
        public static ShapedCurveData CreateInstance(IEnumerable<Vector2> points, IEnumerable<float> shapes, Color color, float width = 3.0f)
        {
            ShapedCurveData result = CreateInstance<ShapedCurveData>();
            result.Init(points, shapes, color, width);
            return result;
        }
        #endregion
    }
}