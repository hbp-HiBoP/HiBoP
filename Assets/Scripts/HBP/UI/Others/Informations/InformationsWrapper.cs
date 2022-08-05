using UnityEngine;
using HBP.Module3D;
using System.Collections.Generic;
using System.Linq;
using HBP.Data.Informations;
using UnityEngine.UI.Extensions;
using UnityEngine.Events;
using Tools.Unity;

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

        [SerializeField] Dictionary<Column3D, Column> m_ColumnDataBy3DColumn = new Dictionary<Column3D, Column>();

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
        public GridInformations GridInformations;

        [SerializeField] Texture2DEvent m_OnChangeColorMap;
        public Texture2DEvent OnChangeColorMap
        {
            get
            {
                return m_OnChangeColorMap;
            }
        }

        bool m_RequestSceneDataUpdate;
        bool m_RequestDisplayUpdate;
        bool m_RequestGraphsUpdate;

        [SerializeField] SceneData m_SceneData;
        [SerializeField] ChannelStruct[] m_FilteredChannelStructs;
        [SerializeField] ChannelStruct[] m_ChannelStructs;
        public ChannelStruct[] ChannelStructs
        {
            get
            {
                return m_ChannelStructs;
            }
        }
        private const int CHANNEL_WARNING_THRESHOLD = 50;
        #endregion

        #region Public Methods
        public void OnExpandHandler()
        {
            m_OnExpand.Invoke();
        }
        public void OnMinimizeHandler()
        {
            m_OnMinimize.Invoke();
        }
        public void ComputeAndDisplayGridGraphs()
        {
            var channelStructs = m_Scene.SelectedColumn.Sites.Where(s => s.State.IsFiltered && !s.State.IsMasked).Select(site => new ChannelStruct(site)).ToArray();
            if (channelStructs.Length > CHANNEL_WARNING_THRESHOLD)
            {
                DialogBoxManager.Open(Tools.Unity.DialogBoxManager.AlertType.WarningMultiOptions, "High number of sites", string.Format("The number of sites you want to display is high ({0}): the recommended value is less than 50. This can cause performance issues. Do you really want to display that many sites?", channelStructs.Length), () => { GridInformations.Display(channelStructs); }, "Display", () => { }, "Cancel");
            }
            else
            {
                GridInformations.Display(channelStructs);
            }
        }
        public void ClearGrid()
        {
            GridInformations.Display(new ChannelStruct[0]);
        }
        public void DisplayChannelsGraphs(ChannelStruct[] channels)
        {
            m_ChannelStructs = channels;
            if (m_ChannelStructs.Length != 0 && m_SceneData.Columns.Count > 0)
            {
                ChannelInformations.DisplayGraphs(m_ChannelStructs, m_SceneData.Columns.ToArray());
            }
        }
        public void FilterChannels(ChannelStruct[] channels)
        {
            foreach (var column in m_Scene.Columns)
            {
                foreach (var site in column.Sites)
                {
                    site.State.IsFiltered = false;
                }
                var sites = column.Sites.Where(s => channels.Any(c => c.Channel == s.name && c.Patient == s.Information.Patient));
                foreach (var site in sites)
                {
                    site.State.IsFiltered = true;
                }
            }
        }
        #endregion

        #region Private Methods
        private void Awake()
        {
            Core.Data.ApplicationState.UserPreferences.OnSavePreferences.AddListener(Display);
        }
        private void Update()
        {
            if (m_RequestSceneDataUpdate)
            {
                GenerateSceneData();
                m_RequestSceneDataUpdate = false;
            }
            if (m_RequestDisplayUpdate)
            {
                Display();
                m_RequestDisplayUpdate = false;
            }
            if (m_RequestGraphsUpdate)
            {
                DisplayGraphs();
                m_RequestGraphsUpdate = false;
            }
        }
        void OnValidate()
        {
            SetBase3DScene();
            SetColorMap();
            SetMinimized();
        }
        void GenerateSceneData()
        {
            if (!m_Scene.SceneInformation.CompletelyLoaded) return;
            List<Column> columns = new List<Column>();
            m_ColumnDataBy3DColumn = new Dictionary<Column3D, Column>();
            foreach (var column in m_Scene.Columns)
            {
                if(!column.IsMinimized || Core.Data.ApplicationState.UserPreferences.Visualization.Graph.ShowCurvesOfMinimizedColumns)
                {
                    List<ChannelStructsGroup> groups = new List<ChannelStructsGroup>();
                    if (m_Scene.ROIManager.SelectedROI != null)
                    {
                        IEnumerable<ChannelStruct> channels = column.Sites.Where(site => !site.State.IsOutOfROI && !site.State.IsMasked && !site.State.IsBlackListed).Select(site => new ChannelStruct(site));
                        groups.Add(new ChannelStructsGroup(m_Scene.ROIManager.SelectedROI.Name, channels.ToList()));
                    }
                    if (m_FilteredChannelStructs.Length > 0)
                    {
                        groups.Add(new ChannelStructsGroup("Filtered", m_FilteredChannelStructs));
                    }
                    if (column is Column3DIEEG ieegColumn)
                    {
                        IEEGData data = new IEEGData(ieegColumn.ColumnIEEGData.Dataset, ieegColumn.ColumnIEEGData.DataName, ieegColumn.ColumnIEEGData.Bloc);
                        Column columnData = new Column(column.Name, data, groups);
                        m_ColumnDataBy3DColumn.Add(column, columnData);
                        columns.Add(columnData);
                    }
                    else if(column is Column3DCCEP ccepColumn && ccepColumn.IsSourceSiteSelected)
                    {
                        CCEPData data = new CCEPData(ccepColumn.ColumnCCEPData.Dataset, ccepColumn.ColumnCCEPData.DataName, new ChannelStruct(ccepColumn.SelectedSourceSite), ccepColumn.ColumnCCEPData.Bloc);
                        Column columnData = new Column(column.Name, data, groups);
                        m_ColumnDataBy3DColumn.Add(column, columnData);
                        columns.Add(columnData);
                    }
                    else if (column is Column3DMEG megColumn)
                    {
                        MEGData data = new MEGData(megColumn.ColumnMEGData.Dataset, megColumn.SelectedMEGItem.Label, megColumn.SelectedMEGItem.Window);
                        Column columnData = new Column(column.Name, data, groups);
                        m_ColumnDataBy3DColumn.Add(column, columnData);
                        columns.Add(columnData);
                    }
                }
            }
            m_SceneData = new SceneData(columns);
            GridInformations.SetColumns(m_SceneData.Columns.ToArray());
        }
        void GenerateChannelStructs(IEnumerable<Core.Object3D.Site> sites)
        {
            m_ChannelStructs = sites.Where(s => !s.State.IsMasked).Select(site => new ChannelStruct(site)).ToArray(); // FIXME: it is better to show a "No data for site X" message instead of filtering by IsMasked
        }
        void GenerateFilteredChannelStructs(IEnumerable<Core.Object3D.Site> sites)
        {
            m_FilteredChannelStructs = sites.Where(site => !site.State.IsMasked).Select(site => new ChannelStruct(site)).ToArray();
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
        void OnSiteInformationRequestHandler(IEnumerable<Core.Object3D.Site> sites)
        {
            GenerateChannelStructs(sites);
            m_RequestSceneDataUpdate = true;
            m_RequestDisplayUpdate = true;
        }
        void OnFilteredSitesRequestHandler(IEnumerable<Core.Object3D.Site> sites)
        {
            GenerateFilteredChannelStructs(sites);
            m_RequestSceneDataUpdate = true;
            m_RequestGraphsUpdate = true;
        }
        void OnMinimizeColumnHandler()
        {
            m_RequestSceneDataUpdate = true;
            m_RequestDisplayUpdate = true;
        }
        void OnChangeSelectedColumn(Column3D column)
        {
            if(column is Column3DDynamic dynamicColumn && Scene.IsGeneratorUpToDate)
            {
                if (column is Column3DCCEP ccepColumn && !ccepColumn.IsSourceSiteSelected) return;

                UpdateTime(dynamicColumn.Timeline.CurrentIndex, dynamicColumn);
            }
        }
        void OnChangeROIHandler()
        {
            m_RequestSceneDataUpdate = true;
            m_RequestGraphsUpdate = true;
        }
        void OnChangeColorMapHandler()
        {
            ColorMap = m_Scene.BrainColorMapTexture;
        }
        void OnChangeSourceHandler()
        {
            m_RequestSceneDataUpdate = true;
            m_RequestDisplayUpdate = true;
        }
        void OnSceneCompletelyLoaded()
        {
            m_RequestSceneDataUpdate = true;
        }
        void OnChangeSelectedMEGItem()
        {
            m_RequestSceneDataUpdate = true;
            m_RequestGraphsUpdate = true;
        }
        void OnChangeSiteState()
        {
            m_RequestSceneDataUpdate = true;
            m_RequestGraphsUpdate = true;
        }
        #endregion

        #region Setters
        void SetBase3DScene()
        {
            if (m_Scene != null)
            {
                m_Scene.OnRequestSiteInformation.AddListener(OnSiteInformationRequestHandler);
                m_Scene.OnRequestFilteredSitesGraph.AddListener(OnFilteredSitesRequestHandler);
                m_Scene.OnChangeColumnMinimizedState.AddListener(OnMinimizeColumnHandler);
                m_Scene.OnUpdateROI.AddListener(OnChangeROIHandler);
                m_Scene.OnChangeColormap.AddListener((t) => OnChangeColorMapHandler());
                m_Scene.OnSelectCCEPSource.AddListener(OnChangeSourceHandler);
                m_Scene.OnSceneCompletelyLoaded.AddListener(OnSceneCompletelyLoaded);
                foreach (var column in m_Scene.Columns)
                {
                    column.OnSelect.AddListener(() => OnChangeSelectedColumn(column));
                    column.OnChangeSiteState.AddListener(s => OnChangeSiteState());
                }
                foreach(var column in m_Scene.ColumnsDynamic)
                {
                    if (column is Column3DCCEP ccepColumn)
                    {
                        ccepColumn.Timeline.OnUpdateCurrentIndex.AddListener(() =>
                        {
                            if (ccepColumn.IsSourceSiteSelected)
                            {
                                UpdateTime(ccepColumn.Timeline.CurrentIndex, ccepColumn);
                            }
                        });
                    }
                    else
                    {
                        column.Timeline.OnUpdateCurrentIndex.AddListener(() => UpdateTime(column.Timeline.CurrentIndex, column));
                    }
                }
                foreach (var column in m_Scene.ColumnsMEG)
                {
                    column.OnChangeSelectedMEG.AddListener(OnChangeSelectedMEGItem);
                }

                int maxNumberOfTrialMatrixColumn = Mathf.Max(Core.Data.Bloc.GetNumberOfColumns(m_Scene.ColumnsIEEG.Select(c => c.ColumnIEEGData.Bloc).Distinct()), Core.Data.Bloc.GetNumberOfColumns(m_Scene.ColumnsCCEP.Select(c => c.ColumnCCEPData.Bloc).Distinct()));
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
        void UpdateTime(int index, Column3DDynamic column)
        {
            if (!m_Scene.SceneInformation.CompletelyLoaded) return;

            if(column.IsSelected && !column.IsMinimized)
            {
                Core.Data.SubBloc subBloc = null;
                foreach (var key in column.Timeline.SubTimelinesBySubBloc.Keys)
                {
                    if (column.Timeline.SubTimelinesBySubBloc[key] == column.Timeline.CurrentSubtimeline)
                    {
                        subBloc = key;
                        break;
                    }
                }
                float currentTime = column.Timeline.CurrentSubtimeline.GetLocalTime(index);
                if (m_ColumnDataBy3DColumn.TryGetValue(column, out Column columnData))
                {
                    ChannelInformations.UpdateTime(columnData, subBloc, currentTime);
                }
            }
        }
        #endregion
    }
}