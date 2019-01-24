using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        private string abscissaUnit;
        public string AbscissaUnit
        {
            get
            {
                return abscissaUnit;
            }
            set
            {
                if (value != abscissaUnit)
                {
                    abscissaUnit = value;
                    OnChangeAbscissaUnit.Invoke(value);
                }
            }
        }
        public StringEvent OnChangeAbscissaUnit;

        private string abscissaLabel;
        public string AbscissaLabel
        {
            get
            {
                return abscissaLabel;
            }
            set
            {
                if(value != abscissaLabel)
                {
                    abscissaLabel = value;
                    OnChangeAbscissaLabel.Invoke(value);
                }
            }
        }
        public StringEvent OnChangeAbscissaLabel;

        private string ordinateUnit;
        public string OrdinateUnit
        {
            get
            {
                return ordinateUnit;
            }
            set
            {
                if (value != ordinateUnit)
                {
                    ordinateUnit = value;
                    OnChangeOrdinateUnit.Invoke(value);
                }
            }
        }
        public StringEvent OnChangeOrdinateUnit;

        private string ordinateLabel;
        public string OrdinateLabel
        {
            get
            {
                return ordinateLabel;
            }
            set
            {
                if(value != ordinateLabel)
                {
                    ordinateLabel = value;
                    OnChangeOrdinateLabel.Invoke(value);
                }
            }
        }
        public StringEvent OnChangeOrdinateLabel;

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

        List<CurveGroup> groups = new List<CurveGroup>();
        public ReadOnlyCollection<CurveGroup> Groups
        {
            get
            {
                return new ReadOnlyCollection<CurveGroup>(groups);
            }
        }
        List<Curve> curves = new List<Curve>();
        public ReadOnlyCollection<Curve> Curves
        {
            get
            {
                return new ReadOnlyCollection<Curve>(curves);
            }
        }

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

        Vector2 ordinateDisplayRange;
        public Vector2 OrdinateDisplayRange
        {
            get
            {
                return ordinateDisplayRange;
            }
            set
            {
                if(value != ordinateDisplayRange)
                {
                    ordinateDisplayRange = value;
                    OnChangeOrdinateDisplayRange.Invoke(value);
                }
            }
        }
        public Vector2 DefaultOrdinateDisplayRange { get; set; }
        public Vector2Event OnChangeOrdinateDisplayRange;

        Vector2 abscissaDisplayRange;
        public Vector2 AbscissaDisplayRange
        {
            get
            {
                return abscissaDisplayRange;
            }
            set
            {
                if(value != abscissaDisplayRange)
                {
                    abscissaDisplayRange = value;
                    OnChangeAbscissaDisplayRange.Invoke(value);
                }
            }
        }
        public Vector2 DefaultAbscissaDisplayRange { get; set; }
        public Vector2Event OnChangeAbscissaDisplayRange;

        bool useDefaultDisplayRange = true;
        public bool UseDefaultDisplayRange
        {
            get
            {
                return useDefaultDisplayRange;
            }
            set
            {
                if(useDefaultDisplayRange != value)
                {
                    useDefaultDisplayRange = value;
                    if(useDefaultDisplayRange)
                    {
                        AbscissaDisplayRange = DefaultAbscissaDisplayRange;
                        OrdinateDisplayRange = DefaultOrdinateDisplayRange;
                    }
                }
            }
        }

        PlotGestion m_PlotGestion;
        InformationsGestion m_InformationsGestion;
        #endregion

        #region Public Methods    
        public void AddGroup(CurveGroup group)
        {
            groups.Add(group);
        }
        public void RemoveGroup(CurveGroup group)
        {
            groups.Remove(group);
        }
        public void AddCurve(CurveData curve, CurveGroup group = null, bool isActive = true)
        {
            CurveGroup groupToAddCurve = groups.Find(g => g == group);
            if(groupToAddCurve != null)
            {
                groupToAddCurve.Curves.Add(new Curve(curve,isActive));
            }
            else
            {
                curves.Add(new Curve(curve, isActive));
            }
        }
        public void RemoveCurve(Curve curve)
        {
            //if(curves.Exists(c => c == curve))
            //{
            //    curves.Remove(curve);
            //}
            //CurveGroup groupToAddCurve = groups.Find(g => g == group);
            //if (groupToAddCurve != null)
            //{
            //    groupToAddCurve.Curves.Add(new Curve(curve, isActive));
            //}
            //else
            //{
            //    curves.Add(new Curve(curve, isActive));
            //}
        }

        public void Plot(GraphData graph, bool useDefaultLimits = true)
        {
            Clear();

            Title = graph.Title;
            AbscissaLabel = graph.Abscissa;
            OrdinateLabel = graph.Ordinate;
            FontColor = graph.FontColor;
            BackgroundColor = graph.BackgroundColor;
            //DefaultLimits = graph.Limits;
            //if (useDefaultLimits) Limits = graph.Limits;
            

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

            //Plot(graph.GroupCurveData, Limits, false);
        }
        public string ToSVG()
        {
            return "";
            //return m_Data.ToSVG();
        }
        public Dictionary<string, string> ToCSV()
        {
            return new Dictionary<string, string>();
            //return m_Data.ToCSV();
        }
        #endregion

        #region Private Methods
        void Clear()
        {

        }
        void Awake()
        {
            //m_PlotGestion = GetComponentInChildren<PlotGestion>();
            //m_InformationsGestion = GetComponentInChildren<InformationsGestion>();
            //m_InformationsGestion.OnDisplayCurve.RemoveAllListeners();
            //m_InformationsGestion.OnDisplayCurve.AddListener((curve, isOn) =>
            //{
            //    curveIsActive[curve.ID] = isOn;
            //    Plot(Groups, Limits);
            //});
            //m_InformationsGestion.OnDisplayGroup.RemoveAllListeners();
            //m_InformationsGestion.OnDisplayGroup.AddListener((group, isOn) =>
            //{
            //    groupIsActive[group.Name] = isOn;
            //    Plot(Groups, Limits);
            //});
            //m_PlotGestion.OnChangeLimits.RemoveAllListeners();
            //m_PlotGestion.OnChangeLimits.AddListener((limits,ignore) => { if(!ignore) useDefaultDisplayRange = false; Plot(Groups, limits, true);});
        }
        void Plot (CurveGroupData[] groupCurves, Limits limits, bool onlyUpdate = false)
        {
            //Groups = groupCurves;
            //SetLimits(limits,true);
            //List<CurveData> curvesToDisplay = new List<CurveData>();
            //foreach (var group in groupCurves)
            //{
            //    if(groupIsActive[group.Name])
            //    {
            //        foreach (var curve in group.Curves)
            //        {
            //            if (curveIsActive[curve.ID]) curvesToDisplay.Add(curve);
            //        }
            //    }
            //}
            //m_PlotGestion.Plot(curvesToDisplay.ToArray(), Limits, onlyUpdate);
        }
        #endregion

        #region Public Structs
        public class CurveGroup
        {
            public string Name { get; set; }
            public bool IsActive { get; set; }
            public List<Curve> Curves { get; set; }
        }
        public class Curve
        {
            public bool IsActive { get; set; }
            public CurveData Data { get; set; }

            public Curve(CurveData data, bool isActive)
            {
                IsActive = isActive;
                Data = data;
            }
        }
        #endregion
    }
}
