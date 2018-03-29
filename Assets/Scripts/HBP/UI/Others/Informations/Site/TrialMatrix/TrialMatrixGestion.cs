using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using HBP.Module3D;
using HBP.UI.TrialMatrix;
using HBP.Data.Experience.Dataset;
using HBP.Data.Experience.Protocol;
using Tools.Unity;

namespace HBP.UI.Informations
{
    public class TrialMatrixGestion : MonoBehaviour
    {
        #region Properties
        [SerializeField] TrialMatrixList m_TrialMatrixList;
        Dictionary<Protocol, ProtocolInformation> m_InformationByProtocol;
        bool m_TrialCanBeSelect = false;
        #endregion

        #region Public Methods
        public void Initialize()
        {
            // OnAutoLimits.
            m_TrialMatrixList.OnAutoLimits.RemoveAllListeners();
            m_TrialMatrixList.OnAutoLimits.AddListener((autoLimits, protocol) => OnChangeAutoLimits(autoLimits, protocol));

            // OnChangeLimits.
            m_TrialMatrixList.OnChangeLimits.RemoveAllListeners();
            m_TrialMatrixList.OnChangeLimits.AddListener((limits, protocol) => OnChangeLimits(limits, protocol));
        }
        public void Set(IEnumerable<Site> sites, Base3DScene scene)
        {
            // Test is a single trial can be selected.
            m_TrialCanBeSelect = sites.All((site) => site.Information.Patient == sites.FirstOrDefault().Information.Patient);

            IEnumerable<Protocol> protocols = (from column in scene.ColumnManager.ColumnsIEEG select column.ColumnData.Protocol).Distinct();
            foreach (Protocol protocol in protocols)
            {
                m_InformationByProtocol[protocol].AutoLimits = true;
                m_InformationByProtocol[protocol].Limits = new Vector2();
            }

            Generate(sites, scene);
            Display(scene.ColumnManager);
        }
        #endregion

        #region Handlers Methods
        public void OnSelectLines(int[] lines, TrialMatrix.Bloc bloc, bool additive)
        {
            if (m_TrialCanBeSelect)
            {
                switch (ApplicationState.GeneralSettings.TrialMatrixSettings.TrialsSynchronization)
                {
                    case Data.Settings.TrialMatrixSettings.TrialsSynchronizationType.Disable:
                        foreach (var trialMatrix in m_TrialMatrixList.TrialMatrix)
                        {
                            foreach (var line in trialMatrix.Lines)
                            {
                                foreach (var b in line.Blocs)
                                {
                                    if (b == bloc)
                                    {
                                        trialMatrix.SelectLines(lines, bloc.Data.ProtocolBloc, additive);
                                        goto @out;
                                    }
                                }
                            }
                        }
                        @out:
                        break;
                    case Data.Settings.TrialMatrixSettings.TrialsSynchronizationType.Enable:
                        foreach (TrialMatrix.TrialMatrix trial in m_TrialMatrixList.TrialMatrix)
                        {
                            trial.SelectLines(lines, bloc.Data.ProtocolBloc, additive);
                        }
                        break;
                    default:
                        break;
                }
            }
        }
        #endregion

        #region Private Methods
        void Generate(IEnumerable<Site> sites, Base3DScene scene)
        {
            // Find protocols to display
            IEnumerable<Protocol> protocols = (from column in scene.ColumnManager.ColumnsIEEG select column.ColumnData.Protocol).Distinct();

            // Generate trialMatrix and create the dictionary
            foreach (Protocol protocol in protocols)
            {
                m_InformationByProtocol[protocol].TrialMatrixByDataInfoBySite = new Dictionary<Site, Dictionary<DataInfo, Data.TrialMatrix.TrialMatrix>>();

                Dictionary<Site, IEnumerable<DataInfo>> dataInfoBySite = sites.ToDictionary(s => s, s => scene.Visualization.GetDataInfo(s.Information.Patient).Where(d => ApplicationState.ProjectLoaded.Datasets.First(ds => ds.Data.Contains(d)).Protocol == protocol));
                IEnumerable<DataInfo> dataInfoToRead = dataInfoBySite.Values.SelectMany(d => d).Distinct();

                Dictionary<DataInfo, Dictionary<Data.Experience.Protocol.Bloc, Data.Localizer.Bloc[]>> epochedBlocsByProtocolBlocByDataInfo = new Dictionary<DataInfo, Dictionary<Data.Experience.Protocol.Bloc, Data.Localizer.Bloc[]>>();
                foreach (var data in dataInfoToRead)
                {
                    Dictionary<Data.Experience.Protocol.Bloc, Data.Localizer.Bloc[]> epochedBlocsByProtocolBloc = new Dictionary<Data.Experience.Protocol.Bloc, Data.Localizer.Bloc[]>();
                    foreach (var bloc in protocol.Blocs)
                    {
                        epochedBlocsByProtocolBloc.Add(bloc, DataManager.GetData(data, bloc).Blocs);
                    }
                    epochedBlocsByProtocolBlocByDataInfo.Add(data, epochedBlocsByProtocolBloc);
                }

                foreach (var site in sites)
                {
                    m_InformationByProtocol[protocol].TrialMatrixByDataInfoBySite[site] = new Dictionary<DataInfo, Data.TrialMatrix.TrialMatrix>();
                    foreach (var dataInfo in dataInfoBySite[site])
                    {
                        Data.TrialMatrix.TrialMatrix trialMatrix = new Data.TrialMatrix.TrialMatrix(protocol, dataInfo, epochedBlocsByProtocolBlocByDataInfo[dataInfo], site, scene);
                        m_InformationByProtocol[protocol].TrialMatrixByDataInfoBySite[site][dataInfo] = trialMatrix;
                    }
                }
            }
        }
        void Display(Column3DManager column3DManager)
        {
            IEnumerable<Protocol> protocols = (from column in column3DManager.ColumnsIEEG where !column.IsMinimized select column.ColumnData.Protocol).Distinct();
            Dictionary<Protocol,ProtocolInformation> informationByProtocol = m_InformationByProtocol.Where(protocol => protocols.Contains(protocol.Key)).ToDictionary(pair => pair.Key, pair => pair.Value);
            Texture2D colorMap = column3DManager.BrainColorMapTexture.RotateTexture();
            colorMap.wrapMode = TextureWrapMode.Clamp;
            m_TrialMatrixList.Set(informationByProtocol, colorMap);
        }
        void OnChangeAutoLimits(bool autoLimits, Protocol protocol)
        {
            m_InformationByProtocol[protocol].AutoLimits = autoLimits;
            if (autoLimits)
            {
                foreach (var trial in m_TrialMatrixList.TrialMatrix)
                {
                    trial.Limits = trial.Data.Limits;
                }
            }
            else
            {
                foreach (var trial in m_TrialMatrixList.TrialMatrix)
                {
                    trial.Limits = m_InformationByProtocol[trial.Data.Protocol].Limits;
                }
            }
        }
        void OnChangeLimits(Vector2 limits, Protocol protocol)
        {
            m_InformationByProtocol[protocol].Limits = limits;
        }
        #endregion

        #region Struct
        public class ProtocolInformation
        {
            public Vector2 Limits;
            public bool AutoLimits;
            public Dictionary<Site, Dictionary<DataInfo, Data.TrialMatrix.TrialMatrix>> TrialMatrixByDataInfoBySite;
        }
        #endregion
    }
}