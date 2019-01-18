using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Tools.Unity.Graph
{
    public class Graph : MonoBehaviour
    {
        #region Properties
        PlotGestion m_PlotGestion;
        InformationsGestion m_InformationsGestion;

        Dictionary<string, bool> m_StateByGroupCurveData = new Dictionary<string, bool>();
        Dictionary<string, bool> m_StateByCurveData = new Dictionary<string, bool>();
        GraphData m_Data = new GraphData();

        public string Title
        {
            get { return m_Data.Title; }
            set
            {
                m_Data.Title = value;
                m_InformationsGestion.SetTitle(value);
            }
        }
        public string Abscissa
        {
            get { return m_Data.Abscissa; }
            set
            {
                m_Data.Abscissa = value;
                m_InformationsGestion.SetAbscissaLabel(value);
            }
        }
        public string Ordinate
        {
            get { return m_Data.Ordinate; }
            set
            {
                m_Data.Ordinate = value;
                m_InformationsGestion.SetOrdinateLabel(value);
            }
        }
        public Color FontColor
        {
            get { return m_Data.Font; }
            set
            {
                m_Data.Font = value;
                m_InformationsGestion.SetColor(value);
                m_PlotGestion.OriginAxeColor = value;
            }
        }
        public Color BackgroundColor
        {
            get { return m_Data.Background; }
            set
            {
                m_Data.Background = value;
                m_PlotGestion.GetComponent<Image>().color = value;
            }
        }
        public GroupCurveData[] GroupCurves
        {
            get { return m_Data.GroupCurveData; }
            private set
            {
                m_Data.GroupCurveData = value;
                Dictionary<GroupCurveData, bool> stateByGroupCurveData = new Dictionary<GroupCurveData, bool>();
                Dictionary<CurveData, bool> stateByCurveData = new Dictionary<CurveData, bool>();
                foreach (var group in value)
                {
                    stateByGroupCurveData.Add(group, m_StateByGroupCurveData[group.Name]);
                    foreach (var curve in group.Curves) stateByCurveData.Add(curve, m_StateByCurveData[curve.ID]);
                }
                m_InformationsGestion.SetLegends(stateByGroupCurveData, stateByCurveData);
            }
        }

        Limits limits;
        public Limits Limits
        {
            get { return limits; }
            private set
            {
                limits = value;
                m_InformationsGestion.SetLimits(value);
            }
        }
        public LimitsEvent OnChangeLimits = new LimitsEvent();

        bool m_AutoLimits = true;
        #endregion

        #region Public Methods    
        public void Plot(GraphData graph)
        {
            m_Data = graph;
            Title = graph.Title;
            Abscissa = graph.Abscissa;
            Ordinate = graph.Ordinate;
            BackgroundColor = graph.Background;
            FontColor = graph.Font;
            List<string> curveToRemove = new List<string>();
            List<string> groupToRemove = new List<string>();
            foreach (var ID in m_StateByCurveData.Keys)
            {
                bool found = false;
                foreach (var group in graph.GroupCurveData)
                {
                    foreach (var curve in group.Curves)
                    {
                        if (curve.ID == ID)
                        {
                            found = true;
                            goto Found;
                        }
                    }
                }
                Found:
                if (!found) curveToRemove.Add(ID);
            }
            foreach (var name in m_StateByGroupCurveData.Keys)
            {
                if(!graph.GroupCurveData.Any(g => g.Name == name))
                {
                    groupToRemove.Add(name);
                }
            }
            foreach (var curve in curveToRemove) m_StateByCurveData.Remove(curve);
            foreach (var group in groupToRemove) m_StateByGroupCurveData.Remove(group);


            foreach (var group in graph.GroupCurveData)
            {
                if(!m_StateByGroupCurveData.ContainsKey(group.Name))
                {
                    m_StateByGroupCurveData[group.Name] = true;
                }
                foreach (var curve in group.Curves)
                {
                    if (!m_StateByCurveData.ContainsKey(curve.ID))
                    {
                        m_StateByCurveData[curve.ID] = true;
                    }
                }
            }


            if (m_AutoLimits)
            {
                Plot(graph.GroupCurveData, graph.Limits, false);
            }
            else
            {
                Plot(graph.GroupCurveData, Limits, false);
            }
        }
        public string ToSVG()
        {
            return m_Data.ToSVG();
        }
        public Dictionary<string, string> ToCSV()
        {
            return m_Data.ToCSV();
        }
        public void SetLimits(Limits limits, bool sendEvent = false)
        {
            Limits = limits;
            if (sendEvent) OnChangeLimits.Invoke(limits);
        }
        #endregion

        #region Private Methods
        void Awake()
        {
            m_PlotGestion = GetComponentInChildren<PlotGestion>();
            m_InformationsGestion = GetComponentInChildren<InformationsGestion>();
            m_InformationsGestion.OnAutoLimits.RemoveAllListeners();
            m_InformationsGestion.OnAutoLimits.AddListener(() => { m_AutoLimits = true; Plot(m_Data); });
            m_InformationsGestion.OnDisplayCurve.RemoveAllListeners();
            m_InformationsGestion.OnDisplayCurve.AddListener((curve, isOn) =>
            {
                m_StateByCurveData[curve.ID] = isOn;
                Plot(GroupCurves, Limits);
            });
            m_InformationsGestion.OnDisplayGroup.RemoveAllListeners();
            m_InformationsGestion.OnDisplayGroup.AddListener((group, isOn) =>
            {
                m_StateByGroupCurveData[group.Name] = isOn;
                Plot(GroupCurves, Limits);
            });
            m_PlotGestion.OnChangeLimits.RemoveAllListeners();
            m_PlotGestion.OnChangeLimits.AddListener((limits,ignore) => { if(!ignore) m_AutoLimits = false; Plot(GroupCurves, limits, true);});
        }
        void Plot (GroupCurveData[] groupCurves, Limits limits, bool onlyUpdate = false)
        {
            GroupCurves = groupCurves;
            Limits = limits;
            List<CurveData> curvesToDisplay = new List<CurveData>();
            foreach (var group in groupCurves)
            {
                if(m_StateByGroupCurveData[group.Name])
                {
                    foreach (var curve in group.Curves)
                    {
                        if (m_StateByCurveData[curve.ID]) curvesToDisplay.Add(curve);
                    }
                }
            }
            m_PlotGestion.Plot(curvesToDisplay.ToArray(), Limits, onlyUpdate);
            m_InformationsGestion.UpdateWindowValues();
        }
        #endregion
    }
}
