using UnityEngine;
using UnityEngine.Events;
using Tools.Unity.ResizableGrid;
using HBP.Module3D;
using System.Collections.Generic;
using System.Linq;
using HBP.Data.Informations;

namespace HBP.UI.Informations
{
    public class InformationsWrapper : MonoBehaviour
    {
        #region Properties
        Base3DScene m_Scene;
        public Base3DScene Scene
        {
            get { return m_Scene; }
            set
            {
                m_Scene = value;
                GenerateDataStructs();
                UpdateColormap();
                m_Scene.OnRequestSiteInformation.AddListener(sites => OnSiteInformationRequest(sites));
                m_Scene.ColumnManager.OnChangeColumnMinimizedState.AddListener(OnChangeColumnMinimizedState);
                m_Scene.OnChangeColormap.AddListener((t) => UpdateColormap());
            }
        }

        public ChannelInformations ChannelInformations;
        public ROIInformations ROIInformations;

        public float MinimumWidth = 200.0f;
        public bool IsMinimized
        {
            get
            {
                return Mathf.Abs(m_RectTransform.rect.width - m_ResizableGrid.MinimumViewWidth) <= MinimumWidth;
            }
        }

        [HideInInspector] public UnityEvent OnOpenInformationsWindow = new UnityEvent();
        [HideInInspector] public UnityEvent OnCloseInformationsWindow = new UnityEvent();

        [SerializeField] RectTransform m_RectTransform;
        [SerializeField] GameObject m_MinimizedGameObject;
        ResizableGrid m_ResizableGrid;

        DataStruct[] m_DataStructs;
        ChannelStruct[] m_ChannelStructs;
        bool m_ShowWholeProtocolLastState;
        #endregion

        #region Public Methods
        public void Open(bool open)
        {
            if(open) OnOpenInformationsWindow.Invoke();
            else OnCloseInformationsWindow.Invoke();
        }
        public void ChangeOverlayState(bool state)
        {
            transform.Find("Borders").Find("BotBorder").gameObject.SetActive(state);
            transform.Find("Borders").Find("LeftBorder").gameObject.SetActive(state);
            transform.Find("Borders").Find("RightBorder").gameObject.SetActive(state);
            transform.Find("Borders").Find("TopBorder").gameObject.SetActive(state);
        }
        #endregion

        #region Private Methods
        void Awake()
        {
            m_ResizableGrid = GetComponentInParent<ResizableGrid>();
            m_ShowWholeProtocolLastState = ApplicationState.UserPreferences.Visualization.TrialMatrix.ShowWholeProtocol;
        }
        void Update()
        {
            if (m_RectTransform.hasChanged)
            {
                m_MinimizedGameObject.SetActive(IsMinimized);
                m_RectTransform.hasChanged = false;
            }
        }

        void DisplayChannels()
        {
            ChannelInformations.Display(m_ChannelStructs, m_DataStructs);
        }
        void OnSiteInformationRequest(IEnumerable<Site> sites)
        {
            if (IsMinimized) Open(true);
            m_ChannelStructs = sites.Select(s => new ChannelStruct(s.Information.ChannelName, s.Information.Patient)).ToArray();
            if (m_ShowWholeProtocolLastState != ApplicationState.UserPreferences.Visualization.TrialMatrix.ShowWholeProtocol) GenerateDataStructs();
            DisplayChannels();
        }
        void OnChangeColumnMinimizedState()
        {
            GenerateDataStructs();
            DisplayChannels();
        }
        void GenerateDataStructs()
        {
            List<DataStruct> dataStructs = new List<DataStruct>();
            foreach (var column in m_Scene.ColumnManager.ColumnsIEEG)
            {
                if (!column.IsMinimized)
                {
                    Data.Visualization.IEEGColumn columnData = column.ColumnIEEGData;
                    DataStruct data;
                    if (dataStructs.Any(d => d.Dataset == columnData.Dataset && d.Data == columnData.DataName))
                    {
                        data = dataStructs.First(d => d.Dataset == columnData.Dataset && d.Data == columnData.DataName);
                    }
                    else
                    {
                        data = new DataStruct(columnData.Dataset, columnData.DataName, new List<Data.Experience.Protocol.Bloc>());
                        dataStructs.Add(data);
                    }
                    data.Blocs.Add(columnData.Bloc);
                }
            }
            if(ApplicationState.UserPreferences.Visualization.TrialMatrix.ShowWholeProtocol)
            {
                for (int d = 0; d < dataStructs.Count; d++)
                {
                    dataStructs[d] = new DataStruct(dataStructs[d].Dataset, dataStructs[d].Data, dataStructs[d].Dataset.Protocol.Blocs.ToList());
                }
            }
            m_DataStructs = dataStructs.ToArray();
        }
        void UpdateColormap()
        {
            ChannelInformations.Colormap = m_Scene.ColumnManager.BrainColorMapTexture;
        }
        #endregion
    }
}