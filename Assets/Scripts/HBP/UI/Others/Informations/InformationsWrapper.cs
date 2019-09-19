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
        //public ROIInformations ROIInformations;

        [SerializeField] Texture2DEvent m_OnChangeColorMap;
        public Texture2DEvent OnChangeColorMap
        {
            get
            {
                return m_OnChangeColorMap;
            }
        }

        [SerializeField] DataStruct[] m_DataStructs;
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
        void GenerateDataStructs()
        {
            if (!m_Scene.IsSceneCompletelyLoaded) return;
            List<DataStruct> dataStructs = new List<DataStruct>();
            foreach (var column in m_Scene.ColumnsIEEG)
            {
                if (!column.IsMinimized || ApplicationState.UserPreferences.Visualization.Graph.ShowCurvesOfMinimizedColumns)
                {
                    Data.Experience.Dataset.Dataset dataset = column.ColumnIEEGData.Dataset;
                    string dataName = column.ColumnIEEGData.DataName;
                    BlocStruct bloc = new BlocStruct(column.ColumnIEEGData.Bloc);
                    IEEGDataStruct data = dataStructs.OfType<IEEGDataStruct>().FirstOrDefault(d => d.Dataset == dataset && d.Data == dataName);
                    if (data == null)
                    {
                        data = new IEEGDataStruct(dataset, dataName);
                        dataStructs.Add(data);
                    }
                    if (!data.Blocs.Any(b => b.Bloc == bloc.Bloc)) data.AddBloc(bloc);
                    if (column.SelectedROI != null)
                    {
                        ROIStruct ROI = new ROIStruct(column.SelectedROI.Name, column.Sites.Where(s => !s.State.IsOutOfROI && !s.State.IsMasked).Select(site => new ChannelStruct(site)));
                        data.Blocs.First(b => b == bloc).AddROI(ROI);
                    }
                }
            }
            foreach (var column in m_Scene.ColumnsCCEP)
            {
                if ((!column.IsMinimized || ApplicationState.UserPreferences.Visualization.Graph.ShowCurvesOfMinimizedColumns) && column.IsSourceSelected)
                {
                    Data.Experience.Dataset.Dataset dataset = column.ColumnCCEPData.Dataset;
                    ChannelStruct source = new ChannelStruct(column.SelectedSource);
                    string dataName = column.ColumnCCEPData.DataName;
                    BlocStruct bloc = new BlocStruct(column.ColumnCCEPData.Bloc);
                    CCEPDataStruct data = dataStructs.OfType<CCEPDataStruct>().FirstOrDefault(d => d.Dataset == dataset && d.Data == dataName && d.Source == source);
                    if (data == null)
                    {
                        data = new CCEPDataStruct(dataset, dataName, source);
                        if(m_ChannelStructs.Any(c => c.Patient == source.Patient))
                        {
                            dataStructs.Add(data);
                        }
                    }
                    if (!data.Blocs.Contains(bloc)) data.AddBloc(bloc);
                    if (column.SelectedROI != null)
                    {
                        ROIStruct ROI = new ROIStruct(column.SelectedROI.Name, column.Sites.Where(s => !s.State.IsOutOfROI && !s.State.IsMasked).Select(site => new ChannelStruct(site)));
                        data.Blocs.First(b => b == bloc).AddROI(ROI);
                    }
                }
            }
            m_DataStructs = dataStructs.ToArray();
        }
        void GenerateChannelStructs(IEnumerable<Site> sites)
        {
            m_ChannelStructs = sites.Where(s => !s.State.IsMasked).Select(site => new ChannelStruct(site)).ToArray(); // FIXME: it is better to show a "No data for site X" message instead of filtering by IsMasked
        }
        void Display()
        {
            if (m_ChannelStructs.Length != 0 && m_DataStructs.Length != 0)
            {
                ChannelInformations.Display(m_ChannelStructs, m_DataStructs);
            }
        }
        #endregion

        #region Handlers
        void OnSiteInformationRequestHandler(IEnumerable<Site> sites)
        {
            GenerateChannelStructs(sites);
            GenerateDataStructs();
            Display();
        }
        void OnMinimizeColumnHandler()
        {
            GenerateDataStructs();
            Display();
        }
        void OnChangeROIHandler()
        {
            GenerateDataStructs();
            Display();
        }
        void OnChangeColorMapHandler()
        {
            ColorMap = m_Scene.BrainColorMapTexture;
        }
        void OnChangeSourceHandler()
        {
            GenerateDataStructs();
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
                GenerateDataStructs();
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