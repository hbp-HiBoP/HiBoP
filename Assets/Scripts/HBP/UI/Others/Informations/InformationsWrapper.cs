using UnityEngine;
using HBP.Module3D;
using System.Collections.Generic;
using System.Linq;
using HBP.Data.Informations;
using UnityEngine.UI.Extensions;
using UnityEngine.Events;
using HBP.Data.Experience.Protocol;

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

        [SerializeField] Texture2D m_ColorMap;
        public Texture2D ColorMap
        {
            get
            {
                return m_ColorMap;
            }
            set
            {
                if (SetPropertyUtility.SetClass(ref m_ColorMap, value))
                {
                    SetColorMap();
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

        [SerializeField] UnityEvent m_OnExpand;
        public UnityEvent OnExpand
        {
            get
            {
                return m_OnExpand;
            }
        }

        [SerializeField] UnityEvent m_OnMinimize;
        public UnityEvent OnMinimize
        {
            get
            {
                return m_OnMinimize;
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
        public ROIInformations ROIInformations;

        [SerializeField] Texture2DEvent m_OnChangeColorMap;
        public Texture2DEvent OnChangeColorMap
        {
            get
            {
                return m_OnChangeColorMap;
            }
        }

        [SerializeField] SceneData m_SceneData;
        [SerializeField] ChannelStruct[] m_ChannelStructs;
        public ChannelStruct[] ChannelStructs
        {
            get
            {
                return m_ChannelStructs;
            }
        }
        #endregion

        #region Public Methods
        public void ROI_DEBUG()
        {
            //Dictionary<Data.Informations.Data, List<Channel>> channelsByData = new Dictionary<Data.Informations.Data, List<Channel>>();
            //foreach (var data in m_SceneData)
            //{
            //}
            //ROIInformations.Display(sceneROIStruct);
        }
        public void OnExpandHandler()
        {
            m_OnExpand.Invoke();
        }
        public void OnMinimizeHandler()
        {
            m_OnMinimize.Invoke();
        }
        #endregion

        #region Private Methods
        void OnValidate()
        {
            SetBase3DScene();
            SetColorMap();
            SetMinimized();
        }
        void GenerateSceneData()
        {
            if (!m_Scene.IsSceneCompletelyLoaded) return;
            List<Column> columns = new List<Column>();
            foreach (var column in m_Scene.Columns)
            {
                if(!column.IsMinimized ||ApplicationState.UserPreferences.Visualization.Graph.ShowCurvesOfMinimizedColumns)
                {
                    Data.Informations.ROI ROI = null;
                    if (m_Scene.ROIManager.SelectedROI != null)
                    {
                        IEnumerable<ChannelStruct> channels = column.Sites.Where(site => !site.State.IsOutOfROI && !site.State.IsMasked).Select(site => new ChannelStruct(site));
                        ROI = new Data.Informations.ROI(m_Scene.ROIManager.SelectedROI.Name, channels.ToList());
                    }
                    if (column is Column3DIEEG ieegColumn)
                    {
                        IEEGData data = new IEEGData(ieegColumn.ColumnIEEGData.Dataset, ieegColumn.ColumnIEEGData.DataName, ieegColumn.ColumnIEEGData.Bloc);
                        columns.Add(new Column(column.Name, data, ROI));
                    }
                    else if(column is Column3DCCEP ccepColumn && ccepColumn.IsSourceSiteSelected)
                    {
                        CCEPData data = new CCEPData(ccepColumn.ColumnCCEPData.Dataset, ccepColumn.ColumnCCEPData.DataName, new ChannelStruct(ccepColumn.SelectedSourceSite), ccepColumn.ColumnCCEPData.Bloc);
                        columns.Add(new Column(column.Name, data, ROI));
                    }
                }
            }
            m_SceneData = new SceneData(columns);
        }
        void GenerateChannelStructs(IEnumerable<Site> sites)
        {
            m_ChannelStructs = sites.Where(s => !s.State.IsMasked).Select(site => new ChannelStruct(site)).ToArray(); // FIXME: it is better to show a "No data for site X" message instead of filtering by IsMasked
        }
        void Display()
        {
            if (m_ChannelStructs.Length != 0 && m_SceneData.Columns.Count != 0)
            {
                ChannelInformations.Display(m_ChannelStructs, m_SceneData.Columns.ToArray());
            }
        }
        void DisplayGraphs()
        {
            if (m_ChannelStructs.Length != 0 && m_SceneData.Columns.Count > 0)
            {
                ChannelInformations.DisplayGraphs(m_ChannelStructs, m_SceneData.Columns.ToArray());
            }
        }
        #endregion

        #region Handlers
        void OnSiteInformationRequestHandler(IEnumerable<Site> sites)
        {
            GenerateChannelStructs(sites);
            GenerateSceneData();
            Display();
        }
        void OnMinimizeColumnHandler()
        {
            GenerateSceneData();
            Display();
        }
        void OnChangeROIHandler()
        {
            GenerateSceneData();
            DisplayGraphs();
        }
        void OnChangeColorMapHandler()
        {
            ColorMap = m_Scene.BrainColorMapTexture;
        }
        void OnChangeSourceHandler()
        {
            GenerateSceneData();
            Display();
        }
        #endregion

        #region Setters
        void SetBase3DScene()
        {
            if (m_Scene != null)
            {
                m_Scene.OnRequestSiteInformation.AddListener(sites => OnSiteInformationRequestHandler(sites));
                m_Scene.OnChangeColumnMinimizedState.AddListener(OnMinimizeColumnHandler);
                m_Scene.OnUpdateROIMask.AddListener(OnChangeROIHandler);
                m_Scene.OnChangeColormap.AddListener((t) => OnChangeColorMapHandler());
                m_Scene.OnSelectCCEPSource.AddListener(OnChangeSourceHandler);

                int maxNumberOfTrialMatrixColumn = Mathf.Max(Bloc.GetNumberOfColumns(m_Scene.ColumnsIEEG.Select(c => c.ColumnIEEGData.Bloc).Distinct()), Bloc.GetNumberOfColumns(m_Scene.ColumnsCCEP.Select(c => c.ColumnCCEPData.Bloc).Distinct()));
                ChannelInformations.SetMaxNumberOfTrialMatrixColumn(maxNumberOfTrialMatrixColumn);
                OnChangeColorMapHandler();

                SetColorMap();
                GenerateSceneData();
            }
        }
        void SetColorMap()
        {
            OnChangeColorMap.Invoke(m_ColorMap);
        }
        void SetMinimized()
        {
            m_OnChangeMinimized.Invoke(m_Minimized);
        }
        #endregion
    }
}