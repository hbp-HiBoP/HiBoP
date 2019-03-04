using UnityEngine;
using HBP.Module3D;
using System.Collections.Generic;
using System.Linq;
using HBP.Data.Informations;
using UnityEngine.UI.Extensions;
using UnityEngine.Events;

namespace HBP.UI.Informations
{
    public class InformationsWrapper : MonoBehaviour
    {
        #region Properties
        [SerializeField] Base3DScene m_Scene;
        public Base3DScene Scene
        {
            get
            {
                return m_Scene;
            }
            set
            {
                if(SetPropertyUtility.SetClass(ref m_Scene, value))
                {
                    SetBase3DScene();
                }
            }
        }

        [SerializeField] bool m_Minimized;
        public bool Minimized
        {
            get
            {
                return m_Minimized;
            }
            set
            {
                if(SetPropertyUtility.SetStruct(ref m_Minimized, value))
                {
                    SetMinimized();
                }
            }
        }

        [SerializeField] BoolEvent m_OnChangeMinimized;
        public BoolEvent OnChangeMinimized
        {
            get
            {
                return m_OnChangeMinimized;
            }
        }

        public ChannelInformations ChannelInformations;
        //public ROIInformations ROIInformations;

        [SerializeField, ReadOnly] Texture2D m_ColorMap;
        [SerializeField] Texture2DEvent m_OnChangeColorMap;
        public Texture2DEvent OnChangeColorMap
        {
            get
            {
                return m_OnChangeColorMap;
            }
        }

        [SerializeField, ReadOnly] DataStruct[] m_DataStructs;
        [SerializeField, ReadOnly] ChannelStruct[] m_ChannelStructs;
        bool m_ShowWholeProtocolLastState;
        #endregion

        #region Private Methods
        void OnValidate()
        {
            SetBase3DScene();
            SetMinimized();
        }
        void Awake()
        {
            m_ShowWholeProtocolLastState = ApplicationState.UserPreferences.Visualization.TrialMatrix.ShowWholeProtocol;
        }
        void DisplayChannels()
        {
            if(m_ChannelStructs != null && m_DataStructs != null && m_ChannelStructs.Length > 0 && m_DataStructs.Length > 0)
            {
                ChannelInformations.Display(m_ChannelStructs, m_DataStructs);
            }
        }
        void OnSiteInformationRequest(IEnumerable<Site> sites)
        {
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
        #endregion

        #region Setters
        void SetBase3DScene()
        {
            if(m_Scene != null)
            {
                GenerateDataStructs();
                SetColorMap();
                m_Scene.OnRequestSiteInformation.AddListener(sites => OnSiteInformationRequest(sites));
                m_Scene.ColumnManager.OnChangeColumnMinimizedState.AddListener(OnChangeColumnMinimizedState);
                m_Scene.OnChangeColormap.AddListener((t) => SetColorMap());
            }
        }
        void SetColorMap()
        {
            m_ColorMap = m_Scene.ColumnManager.BrainColorMapTexture;
            OnChangeColorMap.Invoke(m_ColorMap);
        }
        void SetMinimized()
        {
            m_OnChangeMinimized.Invoke(m_Minimized);
        }
        #endregion
    }
}