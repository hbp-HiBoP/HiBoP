﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using System.Linq;
using UnityEngine.Events;
using data = HBP.Data.TrialMatrix;

namespace HBP.UI.TrialMatrix
{
    public class TrialMatrix : MonoBehaviour
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
                m_Title = value;
                OnChangeTitle.Invoke(value);
            }
        }
        public StringEvent OnChangeTitle;

        Color[] m_Colors;
        private Color[] Colors
        {
            get
            {
                return m_Colors;
            }
            set
            {
                m_Colors = value;
                foreach (var bloc in m_Blocs)
                {
                    bloc.Colors = value;
                }
            }

        }
        Texture2D m_Colormap;
        public Texture2D ColorMap
        {
            get
            {
                return m_Colormap;
            }
            set
            {
                m_Colormap = value;
                Colors = ExtractColormap(value);
                OnChangeColorMap.Invoke(value);
            }
        }
        public Texture2DEvent OnChangeColorMap;

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
                    foreach (SubBloc subBloc in bloc.SubBlocs)
                    {
                        subBloc.Limits = m_Limits;
                    }
                }
            }
        }
        public Vector2Event OnChangeLimits;

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
                    if (value) Limits = Data.Limits;
                    else Limits = m_Limits;
                }
            }
        }
        public BoolEvent OnChangeUsePrecalculatedLimits;

        List<Bloc> m_Blocs = new List<Bloc>();
        public ReadOnlyCollection<Bloc> Blocs
        {
            get { return new ReadOnlyCollection<Bloc>(m_Blocs); }
        }

        public WindowArrayEvent OnChangeTimeLimits;

        [SerializeField] GameObject m_BlocPrefab;
        [SerializeField] RectTransform m_BlocContainer;

        public data.TrialMatrix Data { get; private set; }
        #endregion

        #region Public Methods
        public void Set(data.TrialMatrix data, Texture2D colormap, Vector2 limits = new Vector2())
        {
            Data = data;

            Title = data.Title;
            UsePrecalculatedLimits = limits == new Vector2();
            ColorMap = colormap;

            // Set Legends
            OnChangeTimeLimits.Invoke(data.TimeLimitsByColumn.Select(tuple => tuple.Item2).ToArray());

            //Generate Bloc.
            ClearBlocs();
            foreach (data.Bloc bloc in data.Blocs)
            {
                AddBloc(bloc, m_Colors, Limits);
            }
        }
        #endregion

        #region Private Methods
        void AddBloc(data.Bloc data, Color[] colors, Vector2 limits)
        {
            Bloc bloc = Instantiate(m_BlocPrefab, m_BlocContainer).GetComponent<Bloc>();
            bloc.Set(data, colors, limits);
            bloc.SelectAllTrials();
            m_Blocs.Add(bloc);
        }
        void ClearBlocs()
        {
            foreach (var bloc in Blocs)
            {
               Destroy(bloc.gameObject);
            }
            m_Blocs = new List<Bloc>();
        }
        Color[] ExtractColormap(Texture2D colormap)
        {
            Color[] colors = new Color[colormap.width];
            for (int x = 0; x < colormap.width; x++)
            {
                colors[x] = colormap.GetPixel(x, 0);
            }
            return colors;
        }
        #endregion
    }
}