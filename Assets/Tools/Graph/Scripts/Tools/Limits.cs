using UnityEngine;

namespace Tools.Unity.Graph
{
    public struct Limits
    {
        #region Properties
        public float AbscissaMin { get; set; }
        public float AbscissaMax { get; set; }
        public float OrdinateMin { get; set; }
        public float OrdinateMax { get; set; }
        public Vector2 Abscissa { get { return new Vector2(AbscissaMin, AbscissaMax); } }
        public Vector2 Ordinate { get { return new Vector2(OrdinateMin, OrdinateMax); } }
        public Vector2 Origin { get { return new Vector2(AbscissaMin, OrdinateMin); } }
        #endregion

        #region Constructor
        public Limits(float abscissaMin = 0, float abscissaMax = 0, float ordinateMin = 0, float ordinateMax = 0)
        {
            AbscissaMin = abscissaMin;
            AbscissaMax = abscissaMax;
            OrdinateMin = ordinateMin;
            OrdinateMax = ordinateMax;
        }
        #endregion

        #region Public Methods

        public bool ContainsPoint(Vector2 point, bool inclusive = true)
        {
            return inclusive ?
                point.x >= AbscissaMin && point.x <= AbscissaMax && point.y >= OrdinateMin && point.y <= OrdinateMax :
                point.x > AbscissaMin && point.x < AbscissaMax && point.y > OrdinateMin && point.y < OrdinateMax;
        }
        #endregion

        #region Public Static Methods
        public static bool operator ==(Limits obj1, Limits obj2)
        {
            return
                (obj1.AbscissaMin == obj2.AbscissaMin
                && obj1.AbscissaMax == obj2.AbscissaMax
                && obj1.OrdinateMin == obj2.OrdinateMin
                && obj1.OrdinateMax == obj2.OrdinateMax);
        }
        public static bool operator !=(Limits obj1, Limits obj2)
        {
            return
                !(obj1.AbscissaMin == obj2.AbscissaMin
                && obj1.AbscissaMax == obj2.AbscissaMax
                && obj1.OrdinateMin == obj2.OrdinateMin
                && obj1.OrdinateMax == obj2.OrdinateMax);
        }
        #endregion
    }
}