using HBP.Data.TrialMatrix.Grid;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

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
                    foreach (var bloc in m_Blocs)
                    {
                        bloc.UsePrecalculatedLimits = value;
                    }
                }
            }

        }

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

        [SerializeField] GameObject m_BlocPrefab;
        [SerializeField] RectTransform m_BlocContainer;
        [SerializeField] ValuesLegend m_ValuesLegend;
        #endregion

        #region Public Methods
        public void Set(DataStruct data, Dictionary<ChannelStruct, HBP.Data.TrialMatrix.TrialMatrix> trialMatrixByChannel, Vector2 limits, Texture2D colormap)
        {
            Title = data.Dataset.Name + " " + data.Data;
            Colormap = colormap;
            Limits = limits;

            //foreach (var bloc in data.Dataset.Protocol.Blocs)
            //{
            //    AddBloc(bloc);
            //}
        }
        #endregion

        #region Private Methods
        //void AddBloc(HBP.Data.Experience.Protocol.Bloc data)
        //{
        //    Bloc bloc = (Instantiate(m_BlocPrefab, m_BlocContainer) as GameObject).GetComponent<Bloc>();
        //    bloc.Set(data);
        //    m_Blocs.Add(bloc);
        //}
        #endregion
    }
}