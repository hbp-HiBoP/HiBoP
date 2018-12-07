using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.UI;
using dg = HBP.Data.TrialMatrix.Grid;
using d = HBP.Data.TrialMatrix;
using UnityEngine.Events;

namespace HBP.UI.TrialMatrix.Grid
{
    public class ChannelBloc : MonoBehaviour
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

        public dg.ChannelBloc Data { private set; get; }

        List<SubBloc> m_SubBlocs = new List<SubBloc>();
        public ReadOnlyCollection<SubBloc> SubBlocs { get { return new ReadOnlyCollection<SubBloc>(m_SubBlocs); } }

        List<GameObject> m_Fillers = new List<GameObject>();
        public ReadOnlyCollection<GameObject> Fillers { get { return new ReadOnlyCollection<GameObject>(m_Fillers); } }

        [SerializeField] GameObject m_SubBlocPrefab;
        [SerializeField] RectTransform m_SubBlocContainer;

        RectTransform m_RectTransform;
        LayoutElement m_LayoutElement;
        #endregion

        #region Public Methods
        public void Set(dg.ChannelBloc data, Texture2D colormap, Vector2 limits, IEnumerable<Tuple<int, Tools.CSharp.Window>> timeLimitsByColumn)
        {
            Title = data.Channel.Channel + " (" + data.Channel.Patient.Name + ")";
            Data = data;

            Clear();
            IOrderedEnumerable<HBP.Data.Experience.Protocol.SubBloc> orderedSubBlocs = data.Bloc.OrderedSubBlocs;
            int mainSubBlocIndex = data.Bloc.MainSubBlocPosition;

            foreach (var tuple in timeLimitsByColumn)
            {
                int index = tuple.Item1 + mainSubBlocIndex;
                if (index >= 0 && index < orderedSubBlocs.Count())
                {
                    AddSubBloc(data.SubBlocs[index], colormap, limits, tuple.Item2);
                }
                else
                {
                    AddFiller(tuple.Item2);
                }
            }

            SetSize();
        }
        #endregion

        #region Private Methods    
        void Awake()
        {
            m_LayoutElement = GetComponent<LayoutElement>();
            m_RectTransform = GetComponent<RectTransform>();
        }
        void SetSize()
        {
            switch (ApplicationState.UserPreferences.Visualization.TrialMatrix.SubBlocFormat)
            {
                case HBP.Data.Enums.BlocFormatType.TrialHeight:
                    m_LayoutElement.preferredHeight = ApplicationState.UserPreferences.Visualization.TrialMatrix.TrialHeight * Data.SubBlocs[0].SubTrials.Length;
                    break;
                case HBP.Data.Enums.BlocFormatType.TrialRatio:
                    m_LayoutElement.preferredHeight = ApplicationState.UserPreferences.Visualization.TrialMatrix.TrialRatio * m_RectTransform.rect.width * Data.SubBlocs[0].SubTrials.Length;
                    break;
                case HBP.Data.Enums.BlocFormatType.BlocRatio:
                    m_LayoutElement.preferredHeight = ApplicationState.UserPreferences.Visualization.TrialMatrix.BlocRatio * m_RectTransform.rect.width;
                    break;
            }
        }
        void AddSubBloc(d.SubBloc data, Texture2D colorMap, Vector2 limits, Tools.CSharp.Window window)
        {
            SubBloc subBloc = (Instantiate(m_SubBlocPrefab, m_SubBlocContainer) as GameObject).GetComponent<SubBloc>();
            subBloc.Set(data, colorMap, limits, window);
            m_SubBlocs.Add(subBloc);
        }
        void AddFiller(Tools.CSharp.Window window)
        {
            GameObject filler = new GameObject("Filler");
            filler.transform.SetParent(m_SubBlocContainer);
            Image image = filler.AddComponent<Image>();
            image.sprite = null;
            image.color = new Color(40.0f / 255, 40f / 255, 40f / 255);
            filler.AddComponent<LayoutElement>().flexibleWidth = window.End - window.Start;
            filler.GetComponent<LayoutElement>().flexibleHeight = 1;
            m_Fillers.Add(filler);
        }
        void Clear()
        {
            foreach (var subBloc in m_SubBlocs)
            {
                Destroy(subBloc.gameObject);
            }
            foreach (var filler in m_Fillers)
            {
                Destroy(filler);
            }
            m_SubBlocs = new List<SubBloc>();
            m_Fillers = new List<GameObject>();
        }
        #endregion
    }
}