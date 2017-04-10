using UnityEngine;
using Tools.Unity.Graph;

namespace HBP.UI.Graph
{
    [RequireComponent(typeof(Tools.Unity.Graph.Graph))]
    public class GraphGestion : MonoBehaviour
    {
        #region Properties
        Tools.Unity.Graph.Graph m_graph;
        bool m_setManually = false;
        public bool SetManually { set { m_setManually = value; } get { return m_setManually; } }
        #endregion

        #region Public Methods
        public void Set(CurveData[] curves)
        {
            // Save Limits.
            Limits limits = m_graph.Limits;
            if(!m_setManually)
            {
                limits.OrdinateMin = float.MaxValue;
                limits.OrdinateMax = float.MinValue;
                limits.AbscissaMin = float.MaxValue;
                limits.AbscissaMax = float.MinValue;
                foreach (CurveData curve in curves)
                {
                    if(curve.GetType() == typeof(ShapedCurveData))
                    {
                        ShapedCurveData shapedCurve = curve as ShapedCurveData;
                        for (int i = 0; i < shapedCurve.Points.Length ; i++)
                        {
                            if (limits.AbscissaMin > shapedCurve.Points[i].x) limits.AbscissaMin = shapedCurve.Points[i].x;
                            if (limits.AbscissaMax < shapedCurve.Points[i].x) limits.AbscissaMax = shapedCurve.Points[i].x;
                            if (limits.OrdinateMin > shapedCurve.Points[i].y - shapedCurve.Shapes[i]) limits.OrdinateMin = shapedCurve.Points[i].y - shapedCurve.Shapes[i];
                            if (limits.OrdinateMax < shapedCurve.Points[i].y + shapedCurve.Shapes[i]) limits.OrdinateMax = shapedCurve.Points[i].y + shapedCurve.Shapes[i];
                        }
                    }
                    else
                    {
                        foreach (Vector2 point in curve.Points)
                        {
                            if (limits.AbscissaMin > point.x) limits.AbscissaMin = point.x;
                            if (limits.AbscissaMax < point.x) limits.AbscissaMax = point.x;
                            if (limits.OrdinateMin > point.y) limits.OrdinateMin = point.y;
                            if (limits.OrdinateMax < point.y) limits.OrdinateMax = point.y;
                        }
                    }

                }
            }
            GraphData graphData = new GraphData("EEG", "Time(ms)", "Activity(mV)", Color.black, Color.white, curves, limits);
            m_graph.Plot(graphData);
        }
        #endregion

        #region Private Methods
        void Awake()
        {
            m_graph = GetComponent<Tools.Unity.Graph.Graph>();
            m_graph.OnSetLimitsManually.RemoveAllListeners();
            m_graph.OnSetLimitsManually.AddListener(() => m_setManually = true);
        }
        #endregion
    }

}