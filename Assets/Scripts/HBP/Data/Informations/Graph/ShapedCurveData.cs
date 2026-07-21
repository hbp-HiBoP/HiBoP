using HBP.Core.Preferences;
using System.Collections.Generic;
using UnityEngine;
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
            float[] shapeArray = shapes == null ? System.Array.Empty<float>() : shapes as float[] ?? new List<float>(shapes).ToArray();
            if (shapeArray.Length == Count)
            {
                Shapes = shapeArray;
            }
            else
            {
                Debug.LogWarning("Wrong shape array length");
                Shapes = new float[Count];
            }
        }
        public virtual void InitRegular(float[] values, float[] shapes, float start, float end, Color color, float width)
        {
            base.InitRegular(values, start, end, color, width);
            if (shapes != null && shapes.Length == Count)
                Shapes = shapes;
            else
            {
                Debug.LogWarning("Wrong shape array length");
                Shapes = new float[Count];
            }
        }
        public static CurveData CreateInstance(IEnumerable<Vector2> points, IEnumerable<float> shapes, Color color, float width = 3.0f)
        {
            if (PersistentDataManager.UserPreferences.Visualization.Graph.ShowSEM)
            {
                ShapedCurveData result = CreateInstance<ShapedCurveData>();
                result.Init(points, shapes, color, width);
                return result;
            }
            else
            {
                return CreateInstance(points, color, width);
            }
        }
        public static CurveData CreateRegular(float[] values, float[] shapes, float start, float end, Color color, float width = 3.0f)
        {
            if (PersistentDataManager.UserPreferences.Visualization.Graph.ShowSEM)
            {
                ShapedCurveData result = CreateInstance<ShapedCurveData>();
                result.InitRegular(values, shapes, start, end, color, width);
                return result;
            }
            return CurveData.CreateRegular(values, start, end, color, width);
        }
        #endregion
    }
}
