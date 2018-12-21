using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using d = HBP.Data.TrialMatrix.Grid;

namespace HBP.UI.TrialMatrix.Grid
{
    public class Data : MonoBehaviour
    {
        #region Properties
        string m_Title;
        public string Title
        {
            get
            {
                return m_Title;
            }
            set
            {
                if (value != null && value != m_Title)
                {
                    m_Title = value;
                    OnChangeTitle.Invoke(value);
                }
            }
        }
        public StringEvent OnChangeTitle;

        bool m_UsePrecalculatedLimits;
        public bool UsePrecalculatedLimits
        {
            get
            {
                return m_UsePrecalculatedLimits;
            }
            set
            {
                if (value != m_UsePrecalculatedLimits)
                {
                    m_UsePrecalculatedLimits = value;
                    OnChangeUsePrecalculatedLimits.Invoke(value);
                    if (value) Limits = m_Data.Limits;
                    else Limits = m_Limits;
                }
            }

        }
        public BoolEvent OnChangeUsePrecalculatedLimits;

        Vector2 m_Limits;
        public Vector2 Limits
        {
            get
            {
                return m_Limits;
            }
            set
            {
                m_Limits = value;
                OnChangeLimits.Invoke(value);
                foreach (Bloc bloc in Blocs)
                {
                    bloc.Limits = value;
                }
                if(value != m_Data.Limits)
                {
                    m_UsePrecalculatedLimits = false;
                }
            }
        }
        public Vector2Event OnChangeLimits;

        Texture2D m_Colormap;
        public Texture2D Colormap
        {
            get
            {
                return m_Colormap;
            }
            set
            {
                m_Colormap = value;
                OnChangeColormap.Invoke(value);
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
                m_Colors = value;
                foreach (var bloc in Blocs)
                {
                    bloc.Colors = value;
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
                List<Tools.CSharp.Window> limits = new List<Tools.CSharp.Window>();
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
        void AddTimeLegend(Tools.CSharp.Window[] limits)
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