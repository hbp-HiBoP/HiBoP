using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Tools.Unity.Graph
{
    public class Graph : MonoBehaviour
    {
        #region Properties
        private string title;
        public string Title
        {
            get
            {
                return title;
            }
            set
            {
                if(value != title)
                {
                    title = value;
                    OnChangeTitle.Invoke(value);
                }
            }
        }
        public StringEvent OnChangeTitle;

        private string abscissa;
        public string Abscissa
        {
            get
            {
                return abscissa;
            }
            set
            {
                if(value != abscissa)
                {
                    abscissa = value;
                    OnChangeAbscissa.Invoke(value);
                }
            }
        }
        public StringEvent OnChangeAbscissa;

        private string ordinate;
        public string Ordinate
        {
            get
            {
                return ordinate;
            }
            set
            {
                if(value != ordinate)
                {
                    ordinate = value;
                    OnChangeOrdinate.Invoke(value);
                }
            }
        }
        public StringEvent OnChangeOrdinate;

        private Color fontColor;
        public Color FontColor
        {
            get
            {
                return fontColor;
            }
            set
            {
                if(value != fontColor)
                {
                    fontColor = value;
                    OnChangeFontColor.Invoke(value);
                }
            }
        }
        public ColorEvent OnChangeFontColor;

        private Color backgroundColor;
        public Color BackgroundColor
        {
            get
            {
                return backgroundColor;
            }
            set
            {
                if(value != backgroundColor)
                {
                    backgroundColor = value;
                    OnChangeBackgroundColor.Invoke(value);
                }
            }
        }
        public ColorEvent OnChangeBackgroundColor;


        public CurveGroup[] Groups { get; set; }
        //{
        //    get { return m_Data.GroupCurveData; }
        //    private set
        //    {
        //        m_Data.GroupCurveData = value;
        //        Dictionary<CurveGroupData, bool> stateByGroupCurveData = new Dictionary<CurveGroupData, bool>();
        //        Dictionary<CurveData, bool> stateByCurveData = new Dictionary<CurveData, bool>();
        //        foreach (var group in value)
        //        {
        //            stateByGroupCurveData.Add(group, groupIsActive[group.Name]);
        //            foreach (var curve in group.Curves) stateByCurveData.Add(curve, curveIsActive[curve.ID]);
        //        }
        //        m_InformationsGestion.SetLegends(stateByGroupCurveData, stateByCurveData);
        //    }
        //}


        Limits limits;
        public Limits Limits
        {
            get
            {
                return limits;
            }
            set
            {
                if(value != limits)
                {
                    limits = value;
                    OnChangeLimits.Invoke(value);
                }
            }
        }
        public LimitsEvent OnChangeLimits;

        public Limits DefaultLimits { get; set; }

        bool useDefaultLimits = true;
        public bool UseDefaultLimits
        {
            get
            {
                return useDefaultLimits;
            }
            set
            {
                if(useDefaultLimits != value)
                {
                    useDefaultLimits = value;
                    if(useDefaultLimits)
                    {
                        Limits = DefaultLimits;
                    }
                }
            }
        }

        PlotGestion m_PlotGestion;
        InformationsGestion m_InformationsGestion;

        #endregion

        #region Public Methods    
        public void Plot(GraphData graph, bool useDefaultLimits = true)
        {
            Clear();

            Title = graph.Title;
            Abscissa = graph.Abscissa;
            Ordinate = graph.Ordinate;
            FontColor = graph.FontColor;
            BackgroundColor = graph.BackgroundColor;
            DefaultLimits = graph.Limits;
            if (useDefaultLimits) Limits = graph.Limits;
            

            List<string> curveToRemove = new List<string>();
            List<string> groupToRemove = new List<string>();
            //foreach (var ID in curveIsActive.Keys)
            //{
            //    bool found = false;
            //    foreach (var group in graph.GroupCurveData)
            //    {
            //        foreach (var curve in group.Curves)
            //        {
            //            if (curve.ID == ID)
            //            {
            //                found = true;
            //                goto Found;
            //            }
            //        }
            //    }
            //    Found:
            //    if (!found) curveToRemove.Add(ID);
            //}
            //foreach (var name in groupIsActive.Keys)
            //{
            //    if(!graph.GroupCurveData.Any(g => g.Name == name))
            //    {
            //        groupToRemove.Add(name);
            //    }
            //}
            //foreach (var curve in curveToRemove) curveIsActive.Remove(curve);
            //foreach (var group in groupToRemove) groupIsActive.Remove(group);


            //foreach (var group in graph.GroupCurveData)
            //{
            //    if(!groupIsActive.ContainsKey(group.Name))
            //    {
            //        groupIsActive[group.Name] = true;
            //    }
            //    foreach (var curve in group.Curves)
            //    {
            //        if (!curveIsActive.ContainsKey(curve.ID))
            //        {
            //            curveIsActive[curve.ID] = true;
            //        }
            //    }
            //}

            Plot(graph.GroupCurveData, Limits, false);
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
        void Clear()
        {

        }
        void Awake()
        {
            m_PlotGestion = GetComponentInChildren<PlotGestion>();
            m_InformationsGestion = GetComponentInChildren<InformationsGestion>();
            m_InformationsGestion.OnAutoLimits.RemoveAllListeners();
            m_InformationsGestion.OnAutoLimits.AddListener(() => { useDefaultLimits = true; Plot(m_Data); });
            m_InformationsGestion.OnDisplayCurve.RemoveAllListeners();
            m_InformationsGestion.OnDisplayCurve.AddListener((curve, isOn) =>
            {
                curveIsActive[curve.ID] = isOn;
                Plot(Groups, Limits);
            });
            m_InformationsGestion.OnDisplayGroup.RemoveAllListeners();
            m_InformationsGestion.OnDisplayGroup.AddListener((group, isOn) =>
            {
                groupIsActive[group.Name] = isOn;
                Plot(Groups, Limits);
            });
            m_PlotGestion.OnChangeLimits.RemoveAllListeners();
            m_PlotGestion.OnChangeLimits.AddListener((limits,ignore) => { if(!ignore) useDefaultLimits = false; Plot(Groups, limits, true);});
        }
        void Plot (CurveGroupData[] groupCurves, Limits limits, bool onlyUpdate = false)
        {
            Groups = groupCurves;
            SetLimits(limits,true);
            List<CurveData> curvesToDisplay = new List<CurveData>();
            foreach (var group in groupCurves)
            {
                if(groupIsActive[group.Name])
                {
                    foreach (var curve in group.Curves)
                    {
                        if (curveIsActive[curve.ID]) curvesToDisplay.Add(curve);
                    }
                }
            }
            m_PlotGestion.Plot(curvesToDisplay.ToArray(), Limits, onlyUpdate);
        }
        #endregion

        #region Private Structs
        public struct CurveGroup
        {
            public string Name { get; set; }
            public bool IsActive { get; set; }
            public Curve[] Curves { get; set; }
        }
        public struct Curve
        {
            public bool IsActive { get; set; }
            public CurveData Data { get; set; }
        }
        #endregion
    }
}
