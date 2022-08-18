using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI.Extensions;

namespace HBP.UI.Tools.Graphs
{
    public class LegendsGestion : MonoBehaviour
    {
        #region Properties
        [SerializeField] List<Legend> m_Data = new List<Legend>();
        [SerializeField] List<Graphs.Legend> m_Legends = new List<Graphs.Legend>();

        [SerializeField] GameObject m_LegendPrefab;
        [SerializeField] RectTransform m_Container;

        [SerializeField] StringBoolEvent m_OnChangeEnabled;
        public StringBoolEvent OnChangeEnabled
        {
            get
            {
                return m_OnChangeEnabled;
            }
        }
        #endregion

        #region Public Methods
        public void SetLegends(Legend[] data)
        {
            m_Data = data.ToList();
            SetLegends();
        }
        #endregion

        #region Private Methods
        void Start()
        {
            OnValidate();
        }
        void OnValidate()
        {
            foreach (var item in m_Data)
            {
                item.OnValidate();
            }
            SetLegends();
        }

        void AddLegend(Legend data, RectTransform container)
        {
            Graphs.Legend legend = Instantiate(m_LegendPrefab, container).GetComponent<Graphs.Legend>();
            legend.OnChangeIsActive.AddListener((enabled) => {data.Enabled = enabled;});
            legend.ID = data.ID;
            legend.Color = data.Color;
            legend.Label = data.Label;
            legend.IsActive = data.Enabled;

            data.OnChangeEnabled.AddListener((enabled) => {m_OnChangeEnabled.Invoke(data.ID, data.Enabled);});
            m_Legends.Add(legend);

            foreach (var subLegend in data.SubLegends) AddLegend(subLegend, legend.Container);
        }
        void RemoveLegend(Graphs.Legend legend)
        {
            if (Application.isPlaying)
            {
                Destroy(legend.gameObject);
            }
            else
            {
#if UNITY_EDITOR
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    DestroyImmediate(legend.gameObject);
                };
#endif
            }
            m_Legends.Remove(legend);
            Graphs.Legend[] subLegends = legend.Container.GetComponentsInChildren<Graphs.Legend>();
            foreach (var subLegend in subLegends)
            {
                RemoveLegend(subLegend);
            }
        }
        void UpdateLegend(Legend data)
        {
            Graphs.Legend legend = m_Legends.FirstOrDefault(l => l.ID == data.ID);

            if(legend != null)
            {
                legend.ID = data.ID;
                legend.Color = data.Color;
                legend.Label = data.Label;
                legend.IsActive = data.Enabled;

                foreach (var subLegend in data.SubLegends)
                {
                    UpdateLegend(subLegend);
                }
            }
        }
        void SetLegends()
        {
            Graphs.Legend[] legendsToRemove = m_Legends.Where(l => FindLegendByID(l.ID) == null).ToArray();
            foreach (var legend in legendsToRemove)
            {
                RemoveLegend(legend);
            }
            foreach (var data in m_Data)
            {
                if(!m_Legends.Any(l => l.ID == data.ID))
                {
                    AddLegend(data, m_Container);
                }
                else
                {
                    UpdateLegend(data);
                }
            }
        }
        Legend FindLegendByID(string ID)
        {
            Legend result = null;
            foreach (var legend in m_Data)
            {
                Legend subResult = FindLegendByID(ID, legend);
                if(subResult != null)
                {
                    result = subResult;
                    break;
                }
            }
            return result;
        }
        Legend FindLegendByID(string ID, Legend legend)
        {
            Legend result = null;
            foreach (var subLegend in legend.SubLegends)
            {
                if (subLegend.ID == ID)
                {
                    result = subLegend;
                    break;
                }
                else
                {
                    Legend SubResult = FindLegendByID(ID, subLegend);
                    if (SubResult != null)
                    {
                        result = SubResult;
                        break;
                    }
                }
            }
            return result;
        }
        #endregion

        #region Classes
        [Serializable]
        public class Legend
        {
            #region Properties
            [SerializeField] string m_Label;
            public string Label
            {
                get
                {
                    return m_Label;
                }
                set
                {
                    if (SetPropertyUtility.SetClass(ref m_Label, value))
                    {
                        SetLabel();
                    }
                }
            }

            [SerializeField] Color m_Color;
            public Color Color
            {
                get
                {
                    return m_Color;
                }
                set
                {
                    if (SetPropertyUtility.SetColor(ref m_Color, value))
                    {
                        SetColor();
                    }
                }
            }

            [SerializeField] string m_ID;
            public string ID
            {
                get
                {
                    return m_ID;
                }
                set
                {
                    if (SetPropertyUtility.SetClass(ref m_ID, value))
                    {
                        SetID();
                    }
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
#if UNITY_EDITOR
            [SerializeField]
#endif
            List<Legend> m_SubLegends = new List<Legend>();
            public ReadOnlyCollection<Legend> SubLegends
            {
                get
                {
                    return new ReadOnlyCollection<Legend>(m_SubLegends);
                }
            }

            [SerializeField, HideInInspector] StringEvent m_OnChangeLabel;
            public StringEvent OnChangeLabel
            {
                get
                {
                    return m_OnChangeLabel;
                }
            }

            [SerializeField, HideInInspector] ColorEvent m_OnChangeColor;
            public ColorEvent OnChangeColor
            {
                get
                {
                    return m_OnChangeColor;
                }
            }

            [SerializeField, HideInInspector] StringEvent m_OnChangeID;
            public StringEvent OnChangeID
            {
                get
                {
                    return m_OnChangeID;
                }
            }

            [SerializeField, HideInInspector] BoolEvent m_OnChangeEnabled;
            public BoolEvent OnChangeEnabled
            {
                get
                {
                    return m_OnChangeEnabled;
                }
            }

            [SerializeField, HideInInspector] UnityEvent m_OnChangeSubLegends;
            public UnityEvent OnChangeSubLegends
            {
                get
                {
                    return m_OnChangeSubLegends;
                }
            }
            #endregion

            #region Constructor
            public Legend()
            {
                m_Label = "";
                m_Color = new Color();
                m_ID = "";
                m_Enabled = false;
                m_SubLegends = new List<Legend>();
                m_OnChangeLabel = new StringEvent();
                m_OnChangeColor = new ColorEvent();
                m_OnChangeID = new StringEvent();
                m_OnChangeEnabled = new BoolEvent();
                m_OnChangeSubLegends = new UnityEvent();
            }
            #endregion


            #region Public Methods
            public void AddSubLegend(Legend legendStruct)
            {
                m_SubLegends.Add(legendStruct);
            }
            public void RemoveSubLegend(Legend legendStruct)
            {
                m_SubLegends.Remove(legendStruct);
            }
            public void OnValidate()
            {
                SetLabel();
                SetColor();
                SetID();
                SetEnabled();
                SetSubLegends();
            }
            #endregion

            #region Setters
            void SetLabel()
            {
                m_OnChangeLabel.Invoke(m_Label);
            }
            void SetColor()
            {
                m_OnChangeColor.Invoke(m_Color);
            }
            void SetID()
            {
                m_OnChangeID.Invoke(m_ID);
            }
            void SetEnabled()
            {
                m_OnChangeEnabled.Invoke(m_Enabled);
            }
            void SetSubLegends()
            {
                m_OnChangeSubLegends.Invoke();
            }
            #endregion
        }
        [Serializable] public class StringBoolEvent : UnityEvent<string, bool> { }
        [Serializable] public class LegendsEvent : UnityEvent<Legend[]> { }
        #endregion
    }
}
