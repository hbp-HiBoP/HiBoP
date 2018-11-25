using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using HBP.Module3D;
using HBP.UI.TrialMatrix;
using HBP.Data.Experience.Dataset;
using HBP.Data.Experience.Protocol;
using System;

namespace HBP.UI.Informations
{
    public class TrialMatrixZone : MonoBehaviour
    {
        #region Properties
        [SerializeField] TrialMatrixList m_TrialMatrixList;
        Dictionary<TrialMatrixType,Settings> m_SettingsByTrialMatrixType;
        bool m_TrialCanBeSelect = false;
        bool m_Initialized = false;
        #endregion

        #region Public Methods
        public void Initialize()
        {
            m_SettingsByTrialMatrixType = new Dictionary<TrialMatrixType, Settings>();
            m_TrialCanBeSelect = false;

            m_TrialMatrixList.OnAutoLimitsChanged.AddListener(OnChangeAutoLimits);
            m_TrialMatrixList.OnLimitsChanged.AddListener(OnChangeLimits);

            m_Initialized = true;
        }
        public void Display(IEnumerable<TrialMatrixRequest> trialMatrixRequests)
        {
            if (!m_Initialized) Initialize();

            // Test is a single trial can be selected.
            //m_TrialCanBeSelect = data.All((d) => d.DataInfo.Patient == data.FirstOrDefault().DataInfo.Patient);

            foreach (var request in trialMatrixRequests)
            {
                TrialMatrixType trialMatrixType = new TrialMatrixType(request.DataInfo.Dataset, request.DataInfo.Name);
                Settings settings;
                if (!m_SettingsByTrialMatrixType.TryGetValue(trialMatrixType, out settings))
                {
                    settings = new Settings(new Vector2(), true, new Dictionary<DataInfoStruct, Data.TrialMatrix.TrialMatrix>());
                    m_SettingsByTrialMatrixType.Add(trialMatrixType, settings);
                }

                DataInfoStruct dataInfoStruct = new DataInfoStruct(request.DataInfo, request.Channel);
                //if (!settings.TrialMatrixByDataInfoStruct.ContainsKey(dataInfoStruct))
                //{
                //    settings.TrialMatrixByDataInfoStruct.Add(dataInfoStruct, new Data.TrialMatrix.TrialMatrix(request.DataInfo, request.Channel, request.Blocs));
                //}
            }
            Display();
        }
        #endregion

        #region Handlers Methods
        public void OnSelectTrials(int[] trials, TrialMatrix.Bloc bloc, bool additive)
        {
            //if (m_TrialCanBeSelect)
            //{
            //    if (ApplicationState.UserPreferences.Visualization.TrialMatrix.TrialsSynchronization)
            //    {
            //        foreach (var trialMatrix in m_TrialMatrixList.TrialMatrix)
            //        {
            //            trialMatrix.Blocs.First(b => b == bloc).SelectTrials(trials, additive);
            //        }
            //    }
            //    else
            //    {
            //        foreach (var trialMatrix in m_TrialMatrixList.TrialMatrix)
            //        {
            //            trialMatrix.Blocs.First(b => b == bloc).SelectTrials(trials, additive);
            //        }
            //    }
            //}
        }
        #endregion

        #region Private Methods
        void Display()
        {
            //IEnumerable<Protocol> protocols = (from column in Scene.ColumnManager.ColumnsIEEG where !column.IsMinimized select column.ColumnData.Dataset.Protocol).Distinct();
            //Dictionary<Protocol, Informations> informationByProtocol = m_InformationByPairProtocolAndData.Where(protocol => protocols.Contains(protocol.Key)).ToDictionary(pair => pair.Key, pair => pair.Value);
            //Texture2D colorMap = Scene.ColumnManager.BrainColorMapTexture;
            //colorMap.wrapMode = TextureWrapMode.Clamp;
            //m_TrialMatrixList.Set(informationByProtocol, colorMap);
        }
        void OnChangeAutoLimits(bool autoLimits, TrialMatrixType trialMatrixType)
        {
            Settings settings = m_SettingsByTrialMatrixType[trialMatrixType];
            settings.AutoLimits = autoLimits;
            //if (autoLimits)
            //{
            //    foreach (var trial in m_TrialMatrixList.TrialMatrices)
            //    {
            //    }
            //    foreach (var trial in m_TrialMatrixList.TrialMatrix)
            //    {
            //        trial.Limits = trial.Data.Limits;
            //    }
            //}
            //else
            //{
            //    foreach (var trial in m_TrialMatrixList.TrialMatrix)
            //    {
            //        trial.Limits = m_InformationByPairProtocolAndData[trial.Data.Protocol].Limits;
            //    }
            //}
        }
        void OnChangeLimits(Vector2 limits, TrialMatrixType trialMatrixType)
        {
            Settings settings = m_SettingsByTrialMatrixType[trialMatrixType];
            settings.Limits = limits;
            //foreach (var trial in trialMatrixType)
            //{

            //}
        }
        #endregion

        #region Structs
        public struct Settings
        {
            #region Properties
            public Vector2 Limits { get; set; }
            public bool AutoLimits { get; set; }
            public Dictionary<DataInfoStruct, Data.TrialMatrix.TrialMatrix> TrialMatrixByDataInfoStruct { get; set; }
            #endregion

            #region Constructors
            public Settings(Vector2 limits, bool autoLimits, Dictionary<DataInfoStruct, Data.TrialMatrix.TrialMatrix> trialMatrixByDataInfoStruct)
            {
                Limits = limits;
                AutoLimits = autoLimits;
                TrialMatrixByDataInfoStruct = trialMatrixByDataInfoStruct;
            }
            #endregion
        }
        public struct TrialMatrixRequest
        {
            #region Properties
            public DataInfo DataInfo { get; set; }
            public Data.Experience.Protocol.Bloc[] Blocs { get; set; }
            public string Channel { get; set; }
            #endregion

            #region Constructors
            public TrialMatrixRequest(DataInfo dataInfo, string channel, Data.Experience.Protocol.Bloc[] blocs = null)
            {
                DataInfo = dataInfo;
                Blocs = blocs;
                Channel = channel;
            }
            #endregion
        }
        public struct DataInfoStruct
        {
            #region Properties
            public DataInfo DataInfo { get; set; }
            public string Channel { get; set; }
            #endregion

            #region Constructors
            public DataInfoStruct(DataInfo dataInfo, string channel)
            {
                DataInfo = dataInfo;
                Channel = channel;
            }
            #endregion
        }
        public struct TrialMatrixType
        {
            #region Properties
            public Dataset Dataset { get; set; }
            public string Data { get; set; }
            #endregion

            #region Constructors
            public TrialMatrixType(Dataset dataset, string data) : this()
            {
                Dataset = dataset;
                Data = data;
            }
            #endregion
        }
        #endregion
    }
}