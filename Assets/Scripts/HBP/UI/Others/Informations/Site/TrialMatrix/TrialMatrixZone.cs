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
    public class TrialMatrixZone : MonoBehaviour
    {
        #region Properties
        public Base3DScene Scene { get; set; }
        [SerializeField] TrialMatrixList m_TrialMatrixList;
        Dictionary<Protocol, ProtocolInformation> m_InformationByProtocol;
        bool m_TrialCanBeSelect = false;
        bool m_Initialized = false;
        #endregion

        #region Public Methods
        public void Initialize()
        {
            m_InformationByProtocol = new Dictionary<Protocol, ProtocolInformation>();
            m_TrialCanBeSelect = false;

            m_TrialMatrixList.OnAutoLimits.RemoveListener(OnChangeAutoLimits);
            m_TrialMatrixList.OnAutoLimits.AddListener(OnChangeAutoLimits);

            m_TrialMatrixList.OnChangeLimits.RemoveListener(OnChangeLimits);
            m_TrialMatrixList.OnChangeLimits.AddListener(OnChangeLimits);

            m_Initialized = true;
        }
        public void Display(IEnumerable<Site> sites)
        {
            if (!m_Initialized) Initialize();

            // Test is a single trial can be selected.
            m_TrialCanBeSelect = sites.All((site) => site.Information.Patient == sites.FirstOrDefault().Information.Patient);

            IEnumerable<Protocol> protocols = (from column in Scene.ColumnManager.ColumnsIEEG select column.ColumnData.Dataset.Protocol).Distinct();
            foreach (Protocol protocol in protocols)
            {
                m_InformationByProtocol[protocol] = new ProtocolInformation();
                m_InformationByProtocol[protocol].AutoLimits = true;
                m_InformationByProtocol[protocol].Limits = new Vector2();
            }

            Generate(sites.ToArray());
            Display();
        }
        #endregion

        #region Handlers Methods
        public void OnSelectTrials(int[] trials, TrialMatrix.Bloc bloc, bool additive)
        {
            if (m_TrialCanBeSelect)
            {
                if (ApplicationState.UserPreferences.Visualization.TrialMatrix.TrialsSynchronization)
                {
                    foreach (var trialMatrix in m_TrialMatrixList.TrialMatrix)
                    {
                        trialMatrix.SelectTrials(trials, bloc.Data.ProtocolBloc, additive);
                    }
                }
                else
                {
                    foreach (var trialMatrix in m_TrialMatrixList.TrialMatrix)
                    {
                        foreach (var b in trialMatrix.Blocs)
                        {
                            if(bloc == b)
                            {
                                trialMatrix.SelectTrials(trials, bloc.ProtocolBloc, additive);
                                goto @out;
                            }
                        }
                    }
                    @out:;
                }
            }
        }
        #endregion

        #region Private Methods
        void Generate(Site[] sites)
        {
            //// Find protocols to display
            //IEnumerable<Protocol> protocols = (from column in Scene.ColumnManager.ColumnsIEEG select column.ColumnData.Dataset.Protocol).Distinct();

            //// Generate trialMatrix and create the dictionary
            //foreach (Protocol protocol in protocols)
            //{
            //    m_InformationByProtocol[protocol].TrialMatrixByDataInfoBySite = new Dictionary<Site, Dictionary<DataInfo, Data.TrialMatrix.TrialMatrix>>();

            //    Dictionary<Site, IEnumerable<DataInfo>> dataInfoBySite = sites.ToDictionary(s => s, s => Scene.Visualization.GetDataInfo(s.Information.Patient).Where(d => ApplicationState.ProjectLoaded.Datasets.First(ds => ds.Data.Contains(d)).Protocol == protocol));
            //    IEnumerable<DataInfo> dataInfoToRead = dataInfoBySite.Values.SelectMany(d => d).Distinct();

            //    Dictionary<DataInfo, Dictionary<Data.Experience.Protocol.Bloc, Data.Localizer.Bloc[]>> epochedBlocsByProtocolBlocByDataInfo = new Dictionary<DataInfo, Dictionary<Data.Experience.Protocol.Bloc, Data.Localizer.Bloc[]>>();
            //    foreach (var data in dataInfoToRead)
            //    {
            //        Dictionary<Data.Experience.Protocol.Bloc, Data.Localizer.Bloc[]> epochedBlocsByProtocolBloc = new Dictionary<Data.Experience.Protocol.Bloc, Data.Localizer.Bloc[]>();
            //        foreach (var bloc in protocol.Blocs)
            //        {
            //            epochedBlocsByProtocolBloc.Add(bloc, DataManager.GetData(data, bloc).Blocs);
            //        }
            //        epochedBlocsByProtocolBlocByDataInfo.Add(data, epochedBlocsByProtocolBloc);
            //    }

            //    foreach (var site in sites)
            //    {
            //        m_InformationByProtocol[protocol].TrialMatrixByDataInfoBySite[site] = new Dictionary<DataInfo, Data.TrialMatrix.TrialMatrix>();
            //        foreach (var dataInfo in dataInfoBySite[site])
            //        {
            //            Data.TrialMatrix.TrialMatrix trialMatrix = new Data.TrialMatrix.TrialMatrix(protocol, dataInfo, epochedBlocsByProtocolBlocByDataInfo[dataInfo], site, Scene);
            //            m_InformationByProtocol[protocol].TrialMatrixByDataInfoBySite[site][dataInfo] = trialMatrix;
            //        }
            //    }
            //}
        }
        void Display()
        {
            IEnumerable<Protocol> protocols = (from column in Scene.ColumnManager.ColumnsIEEG where !column.IsMinimized select column.ColumnData.Dataset.Protocol).Distinct();
            Dictionary<Protocol,ProtocolInformation> informationByProtocol = m_InformationByProtocol.Where(protocol => protocols.Contains(protocol.Key)).ToDictionary(pair => pair.Key, pair => pair.Value);
            Texture2D colorMap = Scene.ColumnManager.BrainColorMapTexture.RotateTexture();
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