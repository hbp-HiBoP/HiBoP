using System;
using System.Collections.Generic;
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

        List<Bloc> m_Blocs = new List<Bloc>();
        public Bloc[] Blocs
        {
            get
            {
                return m_Blocs.ToArray();
            }
        }

        d.Data m_Data;

        [SerializeField] GameObject m_BlocPrefab;
        [SerializeField] RectTransform m_BlocContainer;

        [SerializeField] TimeLegend m_TimeLegend;
        #endregion

        #region Public Methods
        public void Set(d.Data data, Texture2D colormap)
        {
            m_Data = data;
            Title = data.Title;
            Colormap = colormap;
            Limits = data.Limits;

            List<Tools.CSharp.Window> windows = new List<Tools.CSharp.Window>();
            foreach (var channel in data.ChannelStructs)
            {
                foreach (var window in data.TimeLimitsByColumn)
                {
                    windows.Add(window.Item2);
                }
            }
            m_TimeLegend.Limits = windows.ToArray();

            foreach (var bloc in data.Blocs)
            {
                AddBloc(bloc, data.TimeLimitsByColumn);
            }
        }
        #endregion

        #region Private Methods
        void AddBloc(d.Bloc data, IEnumerable<Tuple<HBP.Data.Experience.Protocol.SubBloc[], Tools.CSharp.Window>> timeLimitsByColumn)
        {
            Bloc bloc = (Instantiate(m_BlocPrefab, m_BlocContainer) as GameObject).GetComponent<Bloc>();
            bloc.Set(data, m_Colormap, m_Limits, timeLimitsByColumn);
            m_Blocs.Add(bloc);
        }
        #endregion
    }
}