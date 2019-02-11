using System;
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
                if (SetPropertyUtility.SetClass(ref m_OrdinateUnit, value))
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
                if (SetPropertyUtility.SetClass(ref m_OrdinateLabel, value))
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
                if (SetPropertyUtility.SetColor(ref m_FontColor, value))
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
                if (SetPropertyUtility.SetColor(ref m_BackgroundColor, value))
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
                if (SetPropertyUtility.SetStruct(ref m_OrdinateDisplayRange, value))
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
                if (SetPropertyUtility.SetStruct(ref m_DefaultOrdinateDisplayRange, value))
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
                if (SetPropertyUtility.SetStruct(ref m_AbscissaDisplayRange, value))
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
                if (SetPropertyUtility.SetStruct(ref m_DefaultAbscissaDisplayRange, value))
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
                if (SetPropertyUtility.SetStruct(ref m_UseDefaultDisplayRange, value))
                {
                    SetUseDefaultDisplayRange();
                }
            }
        }

        [SerializeField] List<CurveGroup> m_Groups = new List<CurveGroup>();
        public ReadOnlyCollection<CurveGroup> Groups
        {
            get
            {
                return new ReadOnlyCollection<CurveGroup>(m_Groups);
            }
        }

        CurvesDisplayer m_PlotGestion;

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

        [SerializeField] private BoolEvent m_OnChangeUseDefaultRange;
        public BoolEvent OnChangeUseDefaultRange
        {
            get
            {
                return m_OnChangeUseDefaultRange;
            }
        }
        #endregion
        #endregion

        #region Public Methods
        public void SetEnabled(CurveGroupData data, bool enabled)
        {
            CurveGroup group = new CurveGroup(data, enabled);
            m_Groups.Add(group);
        }
        public void AddGroup(CurveGroupData data, bool enabled)
        {
            CurveGroup group = new CurveGroup(data, enabled);
            m_Groups.Add(group);
            SetGroups();
        }
        public void RemoveGroup(CurveGroup group)
        {      
            m_Groups.Remove(group);
            SetGroups();
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
        #endregion

        #region Setters
        void OnValidate()
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
            SetGroups();
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
            if (m_UseDefaultDisplayRange)
            {
                AbscissaDisplayRange = m_DefaultAbscissaDisplayRange;
            }
        }
        void SetDefaultOrdinateDisplayRange()
        {
            if(m_UseDefaultDisplayRange)
            {
                OrdinateDisplayRange = m_DefaultOrdinateDisplayRange;
            }
        }
        void SetUseDefaultDisplayRange()
        {
            if (m_UseDefaultDisplayRange)
            {
                AbscissaDisplayRange = DefaultAbscissaDisplayRange;
                OrdinateDisplayRange = DefaultOrdinateDisplayRange;
            }
        }
        void SetGroups()
        {
            //Debug.Log("SetGroups");

        }
        #endregion

        #region Public Structs
        [Serializable]
        public class CurveGroup
        {
            #region Properties
            [SerializeField] string m_Name;
            public string Name
            {
                get
                {
                    return m_Name;
                }
                set
                {
                    SetPropertyUtility.SetClass(ref m_Name, value);
                }
            }

            [SerializeField] bool m_Enabled;
            public bool Enabled
            {
                get
                {
                    return m_Enabled;
                }
                set
                {
                    if (SetPropertyUtility.SetStruct(ref m_Enabled, value))
                    {
                        SetEnabled();
                    }
                }
            }

            [SerializeField] List<Curve> m_Curves = new List<Curve>();
            public ReadOnlyCollection<Curve> Curves
            {
                get
                {
                    return new ReadOnlyCollection<Curve>(m_Curves);
                }
            }

            #region Event
            [SerializeField] StringEvent m_OnChangeName = new StringEvent();
            public StringEvent OnChangeName
            {
                get
                {
                    return m_OnChangeName;
                }
            }
            
            [SerializeField] BoolEvent m_OnChangeEnabledValue = new BoolEvent();
            public BoolEvent OnChangeEnabledValue
            {
                get
                {
                    return m_OnChangeEnabledValue;
                }
            }

            [SerializeField] Curve.CurveEvent m_OnAddCurve = new Curve.CurveEvent();
            public Curve.CurveEvent OnAddCurve
            {
                get
                {
                    return m_OnAddCurve;
                }
            }

            [SerializeField] Curve.CurveEvent m_OnRemoveCurve = new Curve.CurveEvent();
            public Curve.CurveEvent OnRemoveCurve
            {
                get
                {
                    return m_OnRemoveCurve;
                }
            }
            #endregion
            #endregion

            #region Construtor
            public CurveGroup(CurveGroupData data, bool enabled)
            {
                Name = data.Name;
                Enabled = enabled;
                m_Curves = data.Curves.Select(c => new Curve(c,enabled)).ToList();
            }
            #endregion

            #region Public Methods
            public void AddCurve(Curve curve)
            {
                m_Curves.Add(curve);
                m_OnAddCurve.Invoke(curve);
            }
            public void RemoveCurve(Curve curve)
            {
                if (m_Curves.Remove(curve))
                {
                    m_OnRemoveCurve.Invoke(curve);
                }
            }
            #endregion

            #region Private Methods
            void SetEnabled()
            {
                m_OnChangeEnabledValue.Invoke(m_Enabled);
            }
            #endregion
        }
        [Serializable]
        public class Curve
        {
            #region Properties
            [SerializeField] bool m_Enabled;
            public bool Enabled
            {
                get
                {
                    return m_Enabled;
                }
                set
                {
                    if (SetPropertyUtility.SetStruct(ref m_Enabled, value))
                    {
                        SetEnabled();
                    }
                }
            }

            [SerializeField] CurveData m_Data;
            public CurveData Data
            {
                get
                {
                    return m_Data;
                }
                set
                {
                    if(SetPropertyUtility.SetClass(ref m_Data, value))
                    {
                        SetData();
                    }
                }
            }

            #region Events
            [SerializeField] BoolEvent m_OnChangeEnabledValue = new BoolEvent();
            public BoolEvent OnChangeEnabledValue
            {
                get
                {
                    return m_OnChangeEnabledValue;
                }
            }
            #endregion
            #endregion

            #region Constructor
            public Curve(CurveData data, bool enabled)
            {
                Enabled = enabled;
                Data = data;
            }
            #endregion

            #region Private Methods
            void SetEnabled()
            {
                m_OnChangeEnabledValue.Invoke(m_Enabled);
            }
            void SetData()
            {

            }
            #endregion

            #region Event
            [SerializeField] public class CurveEvent : UnityEvent<Curve> { }
            #endregion
        }
        #endregion
    }
}