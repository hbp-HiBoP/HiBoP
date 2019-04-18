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

        [SerializeField] Texture2D m_ColorMap;
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
            SetMinimized();
        }
        void SiteInformationRequestHandler(IEnumerable<Site> sites)
        {
            GenerateChannelStructs(sites);
            if (m_ChannelStructs.Length != 0 && m_DataStructs.Length != 0)
            {
                ChannelInformations.Display(m_ChannelStructs, m_DataStructs);
            }
        }
        void OnMinimizeColumnHandler()
        {
            GenerateDataStructs();
            if (m_ChannelStructs.Length != 0 && m_DataStructs.Length != 0)
            {
                ChannelInformations.Display(m_ChannelStructs, m_DataStructs);
            }
        }
        void OnChangeROIHandler()
        {
            GenerateDataStructs();
            if (m_ChannelStructs.Length != 0 && m_DataStructs.Length != 0)
            {
                ChannelInformations.Display(m_ChannelStructs, m_DataStructs);
            }
        }

        void GenerateDataStructs()
        {
            List<DataStruct> dataStructs = new List<DataStruct>();
            foreach (var column in m_Scene.ColumnManager.ColumnsIEEG)
            {
                if (!column.IsMinimized || ApplicationState.UserPreferences.Visualization.Graph.ShowCurvesOfMinimizedColumns)
                {
                    Data.Visualization.IEEGColumn columnData = column.ColumnIEEGData;
                    DataStruct data;
                    if (dataStructs.Any(d => d.Dataset == columnData.Dataset && d.Data == columnData.DataName))
                    {
                        data = dataStructs.First(d => d.Dataset == columnData.Dataset && d.Data == columnData.DataName);
                    }
                    else
                    {
                        data = new DataStruct(columnData.Dataset, columnData.DataName, new List<BlocStruct>());
                        dataStructs.Add(data);
                    }
                    if(!data.Blocs.Any(b => b.Bloc == columnData.Bloc))
                    {
                        data.AddBloc(new BlocStruct(columnData.Bloc));
                    }
                    if(column.SelectedROI != null)
                    {
                        ROIStruct ROI = new ROIStruct(column.SelectedROI.Name, column.Sites.Where(s => !s.State.IsOutOfROI && !s.State.IsMasked).Select(s => new ChannelStruct(s.Information.ChannelName, s.Information.Patient)));
                        data.Blocs.First(b => b.Bloc == columnData.Bloc).AddROI(ROI);
                    }
                }
            }
            m_DataStructs = dataStructs.ToArray();
        }
        void GenerateChannelStructs(IEnumerable<Site> sites)
        {
            m_ChannelStructs = sites.Select(s => new ChannelStruct(s.Information.ChannelName, s.Information.Patient)).ToArray();
        }
        void SetGraphZone()
        {
            List<Bloc> blocs = new List<Bloc>();
            foreach (var column in m_Scene.ColumnManager.ColumnsIEEG)
            {
                Bloc bloc = column.ColumnIEEGData.Bloc;
                if(!blocs.Contains(bloc))
                {
                    blocs.Add(bloc);
                }
            }
            int numberOfGraphs = Bloc.GetNumberOfColumns(blocs);
            ChannelInformations.SetMaxNumberOfTrialMatrixColumn(numberOfGraphs);
        }
        #endregion

        #region Setters
        void SetBase3DScene()
        {
            if(m_Scene != null)
            {
                SetGraphZone();
                GenerateDataStructs();
                SetColorMap();
                m_Scene.OnRequestSiteInformation.AddListener(sites => SiteInformationRequestHandler(sites));
                m_Scene.ColumnManager.OnChangeColumnMinimizedState.AddListener(OnMinimizeColumnHandler);
                m_Scene.ColumnManager.OnUpdateROIMask.AddListener(OnChangeROIHandler);
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