using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI.Extensions;

namespace Tools.Unity.Graph
{
    public class LegendsGestion : MonoBehaviour
    {
        #region Properties
        [SerializeField] List<Legend> m_LegendStructs = new List<Legend>();

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
        public void SetLegends(Legend[] legendsStruct)
        {
            m_LegendStructs = legendsStruct.ToList();
            SetLegends();
        }
        #endregion

        #region Private Methods
        private void Start()
        {
            OnValidate();
        }
        void OnValidate()
        {
            foreach (var item in m_LegendStructs)
            {
                item.OnValidate();
            }
            SetLegends();

        }

        void AddLegend(Legend legendStruct, RectTransform container)
        {
            Unity.Graph.Legend legend = Instantiate(m_LegendPrefab, container).GetComponent<Unity.Graph.Legend>();
            UpdateLegend(legend, legendStruct);
        }
        void RemoveLegend(Unity.Graph.Legend legend)
        {
            if (Application.isPlaying)
            {
                Destroy(legend.gameObject);
            }
            else
            {

                UnityEditor.EditorApplication.delayCall += () =>
                {
                    DestroyImmediate(legend.gameObject);
                };
            }
        }
        void UpdateLegend(Unity.Graph.Legend legend, Legend legendStruct)
        {
            legend.OnChangeIsActive.RemoveAllListeners();
            legend.OnChangeIsActive.AddListener((enabled) =>
            {
                legendStruct.Enabled = enabled;
            });

            legendStruct.OnChangeEnabled.RemoveAllListeners();
            legendStruct.OnChangeEnabled.AddListener((enabled) =>
            {
                m_OnChangeEnabled.Invoke(legendStruct.ID, legendStruct.Enabled);
            });

            legend.ID = legendStruct.ID;
            legend.Color = legendStruct.Color;
            legend.Label = legendStruct.Label;
            legend.IsActive = legendStruct.Enabled;

            List<Unity.Graph.Legend> subLegends = new List<Unity.Graph.Legend>();
            foreach (Transform item in legend.Container)
            {
                Unity.Graph.Legend subLegend = item.GetComponent<Unity.Graph.Legend>();
                if (subLegend != null)
                {
                    Legend subStruct = legendStruct.SubLegends.FirstOrDefault(l => l.ID == subLegend.ID);
                    if (subStruct != null)
                    {
                        UpdateLegend(subLegend, subStruct);
                    }
                    else
                    {
                        RemoveLegend(subLegend);
                    }
                    subLegends.Add(subLegend);
                }
            }

            foreach (var subStruct in legendStruct.SubLegends)
            {
                if (!subLegends.Any(s => s.ID == subStruct.ID))
                {
                    AddLegend(subStruct, legend.Container);
                }
            }
        }
        void SetLegends()
        {
            // Update legends.
            List<Unity.Graph.Legend> subLegends = new List<Unity.Graph.Legend>();
            foreach (Transform item in m_Container)
            {
                Unity.Graph.Legend subLegend = item.GetComponent<Unity.Graph.Legend>();
                if (subLegend != null)
                {
                    Legend subStruct = m_LegendStructs.FirstOrDefault(l => l.ID == subLegend.ID);
                    if (subStruct != null)
                    {
                        UpdateLegend(subLegend, subStruct);
                    }
                    else
                    {
                        RemoveLegend(subLegend);
                    }
                    subLegends.Add(subLegend);
                }
            }

            foreach (var subStruct in m_LegendStructs)
            {
                if (!subLegends.Any(s => s.ID == subStruct.ID))
                {
                    AddLegend(subStruct, m_Container);
                }
            }
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

            [SerializeField] List<Legend> m_SubLegends = new List<Legend>();
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
