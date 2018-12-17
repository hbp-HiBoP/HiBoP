using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.UI;
using dg = HBP.Data.TrialMatrix.Grid;
using data = HBP.Data.TrialMatrix;
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
                foreach (var subBloc in m_SubBlocs)
                {
                    subBloc.Colors = value;
                }
            }
        }


        public dg.ChannelBloc Data { private set; get; }

        List<SubBloc> m_SubBlocs = new List<SubBloc>();
        public ReadOnlyCollection<SubBloc> SubBlocs { get { return new ReadOnlyCollection<SubBloc>(m_SubBlocs); } }

        List<GameObject> m_Fillers = new List<GameObject>();
        public ReadOnlyCollection<GameObject> Fillers { get { return new ReadOnlyCollection<GameObject>(m_Fillers); } }

        [SerializeField] GameObject m_SubBlocFillerPrefab;
        [SerializeField] GameObject m_SubBlocPrefab;
        [SerializeField] RectTransform m_SubBlocContainer;

        [SerializeField] GameObject m_SelectionPrefab;
        [SerializeField] RectTransform m_SelectionContainer;

        RectTransform m_RectTransform;
        LayoutElement m_LayoutElement;
        #endregion

        #region Public Methods
        public void Set(dg.ChannelBloc data, Color[] colors, Vector2 limits)
        {
            Title = data.Channel.Channel + " (" + data.Channel.Patient.Name + ")";
            Colors = colors;
            Data = data;

            Clear();
            foreach (var subBloc in data.SubBlocs)
            {
                if(subBloc.IsFiller)
                {
                    AddFiller(subBloc.Window);
                }
                else
                {
                    AddSubBloc(subBloc, colors, limits);
                }
            }
            SetSize();
        }
        #endregion

        #region Private Methods    
        void OnRectTransformDimensionsChange()
        {
            SetSize();
        }
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
        void AddSubBloc(data.SubBloc data, Color[] colors, Vector2 limits)
        {
            SubBloc subBloc = Instantiate(m_SubBlocPrefab, m_SubBlocContainer).GetComponent<SubBloc>();
            subBloc.Set(data, colors, limits);
            m_SubBlocs.Add(subBloc);
        }
        void AddFiller(Tools.CSharp.Window window)
        {
            GameObject filler = Instantiate(m_SubBlocFillerPrefab, m_SubBlocContainer);
            filler.GetComponent<LayoutElement>().flexibleWidth = window.End - window.Start;
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