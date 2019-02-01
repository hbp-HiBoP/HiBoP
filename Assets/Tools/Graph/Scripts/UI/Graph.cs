using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI.Extensions;

namespace Tools.Unity.Graph
{
    public class Graph : MonoBehaviour
    {
        #region Properties
        [SerializeField] private string m_Title;
        public string Title
        {
            get
            {
                return m_Title;
            }
            set
            {
                if (SetPropertyUtility.SetClass(ref m_Title, value))
                {
                    SetTitle();
                }
            }
        }

        [SerializeField] private string m_AbscissaUnit;
        public string AbscissaUnit
        {
            get
            {
                return m_AbscissaUnit;
            }
            set
            {
                if (SetPropertyUtility.SetClass(ref m_AbscissaUnit, value))
                {

                }
            }
        }

        [SerializeField] private string m_AbscissaLabel;
        public string AbscissaLabel
        {
            get
            {
                return m_AbscissaLabel;
            }
            set
            {
                if (SetPropertyUtility.SetClass(ref m_AbscissaLabel, value))
                {
                    SetAbscissaLabel();
                }
            }
        }

        [SerializeField] private string m_OrdinateUnit;
        public string OrdinateUnit
        {
            get
            {
                return m_OrdinateUnit;
            }
            set
            {
                if(SetPropertyUtility.SetClass(ref m_OrdinateUnit, value))
                {
                    SetOrdinateUnit();
                }
            }
        }

        [SerializeField] private string m_OrdinateLabel;
        public string OrdinateLabel
        {
            get
            {
                return m_OrdinateLabel;
            }
            set
            {
                if(SetPropertyUtility.SetClass(ref m_OrdinateLabel, value))
                {
                    OnChangeOrdinateLabel.Invoke(value);
                }
            }
        }

        [SerializeField] private Color m_FontColor;
        public Color FontColor
        {
            get
            {
                return m_FontColor;
            }
            set
            {
                if(SetPropertyUtility.SetColor(ref m_FontColor, value))
                {
                    SetFontColor();
                }
            }
        }

        [SerializeField] private Color m_BackgroundColor;
        public Color BackgroundColor
        {
            get
            {
                return m_BackgroundColor;
            }
            set
            {
                if(SetPropertyUtility.SetColor(ref m_BackgroundColor, value))
                {
                    SetBackgroundColor();
                }
            }
        }

        [SerializeField] private Vector2 m_OrdinateDisplayRange;
        public Vector2 OrdinateDisplayRange
        {
            get
            {
                return m_OrdinateDisplayRange;
            }
            set
            {
                if(SetPropertyUtility.SetStruct(ref m_OrdinateDisplayRange, value))
                {
                    SetOrdinateDisplayRange();
                }
            }
        }

        [SerializeField] private Vector2 m_DefaultOrdinateDisplayRange;
        public Vector2 DefaultOrdinateDisplayRange
        {
            get
            {
                return m_DefaultOrdinateDisplayRange;
            }
            set
            {
                if(SetPropertyUtility.SetStruct(ref m_DefaultOrdinateDisplayRange, value))
                {
                    SetDefaultOrdinateDisplayRange();
                }
            }
        }

        [SerializeField] private Vector2 m_AbscissaDisplayRange;
        public Vector2 AbscissaDisplayRange
        {
            get
            {
                return m_AbscissaDisplayRange;
            }
            set
            {
                if(SetPropertyUtility.SetStruct(ref m_AbscissaDisplayRange, value))
                {
                    SetAbscissaDisplayRange();
                }
            }
        }

        [SerializeField] private Vector2 m_DefaultAbscissaDisplayRange;
        public Vector2 DefaultAbscissaDisplayRange
        {
            get
            {
                return m_DefaultAbscissaDisplayRange;
            }
            set
            {
                if(SetPropertyUtility.SetStruct(ref m_DefaultAbscissaDisplayRange, value))
                {
                    SetDefaultAbscissaDisplayRange();
                }
            }
        }

        [SerializeField] bool m_UseDefaultDisplayRange = true;
        public bool UseDefaultDisplayRange
        {
            get
            {
                return m_UseDefaultDisplayRange;
            }
            set
            {
                if(SetPropertyUtility.SetStruct(ref m_UseDefaultDisplayRange, value))
                {
                    SetUseDefaultDisplayRange();
                }
            }
        }

        PlotGestion m_PlotGestion;
        InformationsGestion m_InformationsGestion;

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

        #region Events
        [SerializeField] private StringEvent m_OnChangeTitle;
        public StringEvent OnChangeTitle
        {
            get
            {
                return m_OnChangeTitle;
            }
        }

        [SerializeField] private StringEvent m_OnChangeAbscissaUnit;
        public StringEvent OnChangeAbscissaUnit
        {
            get
            {
                return m_OnChangeAbscissaUnit;
            }
        }

        [SerializeField] private StringEvent m_OnChangeAbscissaLabel;
        public StringEvent OnChangeAbscissaLabel
        {
            get
            {
                return m_OnChangeAbscissaLabel;
            }
        }

        [SerializeField] private StringEvent m_OnChangeOrdinateUnit;
        public StringEvent OnChangeOrdinateUnit
        {
            get
            {
                return m_OnChangeOrdinateUnit;
            }
        }

        [SerializeField] private StringEvent m_OnChangeOrdinateLabel;
        public StringEvent OnChangeOrdinateLabel
        {
            get
            {
                return m_OnChangeOrdinateLabel;
            }
        }

        [SerializeField] private ColorEvent m_OnChangeFontColor;
        public ColorEvent OnChangeFontColor
        {
            get
            {
                return m_OnChangeFontColor;
            }
        }

        [SerializeField] private ColorEvent m_OnChangeBackgroundColor;
        public ColorEvent OnChangeBackgroundColor
        {
            get
            {
                return m_OnChangeBackgroundColor;
            }
        }

        [SerializeField] private Vector2Event m_OnChangeOrdinateDisplayRange;
        public Vector2Event OnChangeOrdinateDisplayRange
        {
            get
            {
                return m_OnChangeOrdinateDisplayRange;
            }
        }

        [SerializeField] private Vector2Event m_OnChangeAbscissaDisplayRange;
        public Vector2Event OnChangeAbscissaDisplayRange
        {
            get
            {
                return m_OnChangeAbscissaDisplayRange;
            }
        }
        #endregion

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
            if (groupToAddCurve != null)
            {
                groupToAddCurve.Curves.Add(new Curve(curve, isActive));
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
        void Plot(CurveGroupData[] groupCurves, Limits limits, bool onlyUpdate = false)
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

        #region Setters
        private void OnValidate()
        {
            SetTitle();
            SetAbscissaUnit();
            SetAbscissaLabel();
            SetOrdinateUnit();
            SetOrdinateLabel();
            SetFontColor();
            SetBackgroundColor();
            SetOrdinateDisplayRange();
            SetAbscissaDisplayRange();
        }
        void SetTitle()
        {
            m_OnChangeTitle.Invoke(m_Title);
        }
        void SetAbscissaUnit()
        {
            m_OnChangeAbscissaUnit.Invoke(m_AbscissaUnit);
        }
        void SetAbscissaLabel()
        {
            m_OnChangeAbscissaLabel.Invoke(m_AbscissaLabel);
        }
        void SetOrdinateUnit()
        {
            m_OnChangeOrdinateUnit.Invoke(m_OrdinateUnit);
        }
        void SetOrdinateLabel()
        {
            m_OnChangeOrdinateLabel.Invoke(m_OrdinateLabel);
        }
        void SetFontColor()
        {
            m_OnChangeFontColor.Invoke(m_FontColor);
        }
        void SetBackgroundColor()
        {
            m_OnChangeBackgroundColor.Invoke(m_BackgroundColor);
        }
        void SetOrdinateDisplayRange()
        {
            m_OnChangeOrdinateDisplayRange.Invoke(m_OrdinateDisplayRange);
        }
        void SetAbscissaDisplayRange()
        {
            m_OnChangeAbscissaDisplayRange.Invoke(m_AbscissaDisplayRange);
        }
        void SetDefaultAbscissaDisplayRange()
        {

        }
        void SetDefaultOrdinateDisplayRange()
        {

        }
        void SetUseDefaultDisplayRange()
        {
            if (m_UseDefaultDisplayRange)
            {
                AbscissaDisplayRange = DefaultAbscissaDisplayRange;
                OrdinateDisplayRange = DefaultOrdinateDisplayRange;
            }
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
