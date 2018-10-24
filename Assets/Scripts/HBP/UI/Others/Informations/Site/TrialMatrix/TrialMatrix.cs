using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        Texture2D m_Colormap;
        public Texture2D ColorMap
        {
            get
            {
                return m_Colormap;
            }
            set
            {
                DestroyImmediate(m_Colormap);
                m_Colormap = value;
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
                m_UsePrecalculatedLimits = value;
                if (value) Limits = Data.Limits;
                else Limits = m_Limits;
            }
        }
        public BoolEvent OnChangeUsePrecalculatedLimits;

        List<Bloc> m_Blocs = new List<Bloc>();
        public ReadOnlyCollection<Bloc> Blocs
        {
            get { return new ReadOnlyCollection<Bloc>(m_Blocs); }
        }

        public Vector2ArrayEvent OnChangeTimeLimits;

        [SerializeField] GameObject m_BlocPrefab;
        [SerializeField] RectTransform m_BlocsRectTransform;

        public data.TrialMatrix Data { get; private set; }
        #endregion

        #region Public Methods
        public void Set(data.TrialMatrix trialMatrix , Texture2D colorMap, Vector2 limits = new Vector2())
        {
            // TODO
            //m_Data = trialMatrix;

            //Title = trialMatrix.Title;
            //ColorMap = colorMap;
            //UsePrecalculatedLimits = limits == new Vector2();

            ////Organize array
            //data.Bloc[] l_blocs = trialMatrix.Blocs.OrderBy(t => t.ProtocolBloc.Position.Row).ThenBy(t => t.ProtocolBloc.Position.Column).ToArray();

            //// Set Legends
            //OnChangeTimeLimits.Invoke(trialMatrix.TimeLimitsByColumn);

            ////Separate blocs by line
            //List<data.Bloc[]> l_lines = new List<data.Bloc[]>();
            //foreach (var bloc in l_blocs)
            //{
            //    if (!l_lines.Exists((a) => a.Contains(bloc)))
            //    {
            //        l_lines.Add(System.Array.FindAll(l_blocs, x => x.ProtocolBloc.Position.Row == bloc.ProtocolBloc.Position.Row));
            //    }
            //}


            //int maxBlocByRow = 0;
            //foreach (data.Bloc[] line in l_lines)
            //{
            //        int max = line[line.Length - 1].ProtocolBloc.Position.Column;
            //        if (max > maxBlocByRow)
            //        {
            //            maxBlocByRow = max;
            //        }
            //}

            ////Generate Line
            //for (int i = 0; i < l_lines.Count; i++)
            //{
            //    AddLine(l_lines[i], maxBlocByRow, m_Colormap, Limits);
            //}
            //SelectAllLines();
        }
        #endregion

        #region Private Methods
        void AddBloc(data.Bloc data, Texture2D colorMap, Vector2 limits)
        {
            Bloc bloc = Instantiate(m_BlocPrefab, m_BlocsRectTransform).GetComponent<Bloc>();
            bloc.Set(data, colorMap, limits);
            m_Blocs.Add(bloc);
        }
        #endregion
    }
}