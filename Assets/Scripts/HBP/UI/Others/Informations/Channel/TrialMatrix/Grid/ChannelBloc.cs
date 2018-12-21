using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using UnityEngine.UI;
using dg = HBP.Data.TrialMatrix.Grid;
using data = HBP.Data.TrialMatrix;
using UnityEngine.Events;
using System.Linq;

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

        public bool IsHovered
        {
            get
            {
                return SubBlocs.Any(s => s.IsHovered);
            }
        }
        public UnityEvent OnChangeIsHovered;

        public dg.ChannelBloc Data { private set; get; }

        public SubBloc SubBlocHovered
        {
            get
            {
                return SubBlocs.FirstOrDefault(c => c.IsHovered);
            }
        }
        List<SubBloc> m_SubBlocs = new List<SubBloc>();
        public ReadOnlyCollection<SubBloc> SubBlocs { get { return new ReadOnlyCollection<SubBloc>(m_SubBlocs); } }

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
            foreach (var subBloc in data.SubBlocs) AddSubBloc(subBloc, colors, limits);
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
            if(Data.IsFound)
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
            else
            {
                m_LayoutElement.flexibleHeight = 1;
            }
        }
        void AddSubBloc(data.SubBloc data, Color[] colors, Vector2 limits)
        {
            SubBloc subBloc = Instantiate(m_SubBlocPrefab, m_SubBlocContainer).GetComponent<SubBloc>();
            subBloc.Set(data, colors, limits);
            subBloc.OnChangeIsHovered.AddListener(() => OnChangeIsHovered.Invoke());
            m_SubBlocs.Add(subBloc);
        }
        void Clear()
        {
            foreach (var subBloc in m_SubBlocs)
            {
                Destroy(subBloc.gameObject);
            }
            m_SubBlocs = new List<SubBloc>();
        }
        #endregion
    }
}