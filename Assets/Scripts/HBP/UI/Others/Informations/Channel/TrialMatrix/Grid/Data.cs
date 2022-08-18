using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI.Extensions;
using d = HBP.Display.Informations.TrialMatrix.Grid;

namespace HBP.UI.Module3D.Informations.TrialMatrix.Grid
{
    public class Data : MonoBehaviour
    {
        #region Properties
        [SerializeField] string m_Title;
        public string Title
        {
            get
            {
                return m_Title;
            }
            set
            {
                if(SetPropertyUtility.SetClass(ref m_Title, value))
                {
                    OnChangeTitle.Invoke(value);
                }
            }
        }
        public StringEvent OnChangeTitle;

        [SerializeField] bool m_UseDefaultLimits;
        public bool UseDefaultLimits
        {
            get
            {
                return m_UseDefaultLimits;
            }
            set
            {
                if(SetPropertyUtility.SetStruct(ref m_UseDefaultLimits, value))
                { 
                    OnChangeUseDefaultLimits.Invoke(value);
                    if (value) Limits = m_Data.Limits;
                    else Limits = m_Limits;
                }
            }

        }
        public BoolEvent OnChangeUseDefaultLimits;

        [SerializeField] Vector2 m_Limits;
        public Vector2 Limits
        {
            get
            {
                return m_Limits;
            }
            set
            {
                if(SetPropertyUtility.SetStruct(ref m_Limits,value))
                {
                    OnChangeLimits.Invoke(value);
                    foreach (Bloc bloc in Blocs)
                    {
                        bloc.Limits = value;
                    }
                    m_UseDefaultLimits = value == m_Data.Limits;
                }
            }
        }
        public Vector2Event OnChangeLimits;

        [SerializeField] Texture2D m_Colormap;
        public Texture2D Colormap
        {
            get
            {
                return m_Colormap;
            }
            set
            {
                if(SetPropertyUtility.SetClass(ref m_Colormap, value))
                {
                    OnChangeColormap.Invoke(value);
                }
            }
        }
        public Texture2DEvent OnChangeColormap;

        public bool IsHovered
        {
            get
            {
                return Blocs.Any(b => b.IsHovered);
            }
        }
        public BoolEvent OnChangeIsHovered;

        Color[] m_Colors;
        public Color[] Colors
        {
            get
            {
                return m_Colors;
            }
            set
            {
                if(SetPropertyUtility.SetClass(ref m_Colors, value))
                {
                    foreach (var bloc in Blocs)
                    {
                        bloc.Colors = value;
                    }
                } 
            }
        }

        public Bloc BlocHovered
        {
            get
            {
                return Blocs.FirstOrDefault(b => b.IsHovered);
            }
        }
        List<Bloc> m_Blocs = new List<Bloc>();
        public ReadOnlyCollection<Bloc> Blocs
        {
            get
            {
                return new ReadOnlyCollection<Bloc>(m_Blocs);
            }
        }

        d.Data m_Data;
        public d.Data GridData
        {
            get
            {
                return m_Data;
            }
        }

        [SerializeField] GameObject m_BlocPrefab;
        [SerializeField] RectTransform m_BlocContainer;

        List<TimeLegend> m_TimeLegends = new List<TimeLegend>();
        [SerializeField] GameObject m_TimeLegendPrefab;
        [SerializeField] RectTransform m_TimeLegendContainer; 
        #endregion

        #region Public Methods
        public void Set(d.Data data, Texture2D colormap, Color[] colors)
        {
            m_Data = data;
            Title = data.Title;
            Colormap = colormap;
            Colors = colors;
            Limits = data.Limits;
            Clear();

            foreach (var channel in data.ChannelStructs)
            {
                List< Core.Tools.TimeWindow > limits = new List<Core.Tools.TimeWindow>();
                foreach (var tuple in data.SubBlocsAndWindowByColumn)
                {
                    limits.Add(tuple.Item2);
                }
                AddTimeLegend(limits.ToArray());
            }

            foreach (var bloc in data.Blocs)
            {
                AddBloc(bloc);
            }
        }
        #endregion

        #region Private Methods
        void Clear()
        {
            foreach (var timeLegend in m_TimeLegends)
            {
                Destroy(timeLegend.gameObject);
            }
            foreach (var bloc in m_Blocs)
            {
                Destroy(bloc.gameObject);
            }
            m_TimeLegends = new List<TimeLegend>();
            m_Blocs = new List<Bloc>();
        }
        void AddTimeLegend(Core.Tools.TimeWindow[] limits)
        {
            TimeLegend timeLegend = Instantiate(m_TimeLegendPrefab, m_TimeLegendContainer).GetComponent<TimeLegend>();
            timeLegend.Limits = limits;
            m_TimeLegends.Add(timeLegend);
        }
        void AddBloc(d.Bloc data)
        {
            Bloc bloc = (Instantiate(m_BlocPrefab, m_BlocContainer) as GameObject).GetComponent<Bloc>();
            bloc.Set(data, Colors, Limits);
            bloc.OnChangeIsHovered.AddListener(() => OnChangeIsHovered.Invoke(IsHovered));
            m_Blocs.Add(bloc);
        }
        #endregion
    }
}