using UnityEngine;
using System.Linq;
using System.Collections.Generic;

namespace Tools.Unity.Graph
{
    public class CurveData
    {
        #region Properties
        public string ID { get; protected set; }
        public string Name { get; set; }
        public Vector2[] Points { get; set; }
        public Color Color { get; set; }
        public float Width { get; set; }
        #endregion

        #region Constructor
        public CurveData(string name, string id, IEnumerable<Vector2> points, Color color, float width = 3.0f)
        {
            ID = id;
            Name = name;
            Color = color;
            Points = points.ToArray();
            Width = width;
        }
        #endregion
    }
}