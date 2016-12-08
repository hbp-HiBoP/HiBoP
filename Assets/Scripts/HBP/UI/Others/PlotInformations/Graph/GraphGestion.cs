using UnityEngine;
using System.Linq;
using Tools.Unity.Graph;
using System.Collections.Generic;
using d = Tools.Unity.Graph.Data;

namespace HBP.UI.Graph
{
    [RequireComponent(typeof(Tools.Unity.Graph.Graph))]
    public class GraphGestion : MonoBehaviour
    {
        #region Properties
        Tools.Unity.Graph.Graph m_graph;
        Vector2 m_abcissaWindow;
        Vector2 m_ordinateWindow; 
        bool m_setManually = false;
        public bool SetManually { set { m_setManually = value; } get { return m_setManually; } }
        #endregion

        #region Public Methods
        public void Set(d.Curve[] curves)
        {
            // Save Limits.
            m_abcissaWindow = m_graph.AbcissaWindow;
            m_ordinateWindow = m_graph.OrdinateWindow;
            if(!m_setManually)
            {
                m_abcissaWindow = new Vector2(float.MaxValue, float.MinValue);
                m_ordinateWindow = new Vector2(float.MaxValue, float.MinValue);
                foreach (d.Curve curve in curves)
                {
                    foreach (Vector2 point in curve.Points)
                    {
                        if (m_abcissaWindow.x > point.x) m_abcissaWindow.x = point.x;
                        if (m_abcissaWindow.y < point.x) m_abcissaWindow.y = point.x;
                        if (m_ordinateWindow.x > point.y) m_ordinateWindow.x = point.y;
                        if (m_ordinateWindow.y < point.y) m_ordinateWindow.y = point.y;
                    }
                }
            }

            // Set Graph.
            m_graph.Set("EEG","Time(ms)", "Activity(mV)", Color.black, Color.white);

            m_graph.Display(curves,m_abcissaWindow, m_ordinateWindow, true);
        }
        #endregion

        #region Private Methods
        void Awake()
        {
            m_graph = GetComponent<Tools.Unity.Graph.Graph>();
            m_graph.SetWindowEvent.RemoveAllListeners();
            m_graph.SetWindowEvent.AddListener(() => m_setManually = true);
        }
        #endregion
    }

}