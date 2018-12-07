using UnityEngine;
using System.Collections.Generic;
using HBP.UI.TrialMatrix;
using HBP.Data.Experience.Dataset;
using data = HBP.Data.Informations;
using System.Linq;

namespace HBP.UI.Informations
{
    public class TrialMatrixZone : MonoBehaviour
    {
        #region Properties
        [SerializeField] Texture2D m_Colormap;
        [SerializeField] TrialMatrixList m_TrialMatrixList;

        data.ChannelStruct[] m_ChannelStructs;
        data.DataStruct[] m_DataStructs;
        bool TrialCanBeSelect
        {
            get
            {
                return m_ChannelStructs.All((c) => c.Patient == m_ChannelStructs.FirstOrDefault().Patient);
            }
        }
        Dictionary<data.DataStruct, Settings> m_SettingsByData;
        #endregion

        #region Public Methods
        public void Display(data.ChannelStruct[] channelStructs, data.DataStruct[] dataStructs)
        {
            m_ChannelStructs = channelStructs;
            m_DataStructs = dataStructs;

            foreach (var data in dataStructs)
            {
                Settings settings = new Settings(new Vector2(), true, new Dictionary<data.ChannelStruct, Data.TrialMatrix.TrialMatrix>());
                foreach (var channel in channelStructs)
                {
                    DataInfo dataInfo = data.Dataset.Data.FirstOrDefault(d => d.Patient == channel.Patient && d.Name == data.Data);
                    if(dataInfo != null)
                    {
                        settings.TrialMatrixByChannel.Add(channel, new Data.TrialMatrix.TrialMatrix(dataInfo, channel.Channel, data.Blocs));
                    }
                }
                m_SettingsByData.Add(data, settings);
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
        void Awake()
        {
            m_SettingsByData = new Dictionary<data.DataStruct, Settings>();

            //m_TrialMatrixList.OnAutoLimitsChanged.AddListener(OnChangeAutoLimits);
            //m_TrialMatrixList.OnLimitsChanged.AddListener(OnChangeLimits);
        }
        void Display()
        {
            //IEnumerable<Protocol> protocols = (from column in Scene.ColumnManager.ColumnsIEEG where !column.IsMinimized select column.ColumnData.Dataset.Protocol).Distinct();
            //Dictionary<Protocol, Informations> informationByProtocol = m_InformationByPairProtocolAndData.Where(protocol => protocols.Contains(protocol.Key)).ToDictionary(pair => pair.Key, pair => pair.Value);
            //Texture2D colorMap = Scene.ColumnManager.BrainColorMapTexture;
            //colorMap.wrapMode = TextureWrapMode.Clamp;
            //m_TrialMatrixList.Set(informationByProtocol, colorMap);
        }
        //void OnChangeAutoLimits(bool autoLimits, TrialMatrixType trialMatrixType)
        //{
        //    Settings settings = m_SettingsByData[trialMatrixType];
        //    settings.AutoLimits = autoLimits;
        //    //if (autoLimits)
        //    //{
        //    //    foreach (var trial in m_TrialMatrixList.TrialMatrices)
        //    //    {
        //    //    }
        //    //    foreach (var trial in m_TrialMatrixList.TrialMatrix)
        //    //    {
        //    //        trial.Limits = trial.Data.Limits;
        //    //    }
        //    //}
        //    //else
        //    //{
        //    //    foreach (var trial in m_TrialMatrixList.TrialMatrix)
        //    //    {
        //    //        trial.Limits = m_InformationByPairProtocolAndData[trial.Data.Protocol].Limits;
        //    //    }
        //    //}
        //}
        //void OnChangeLimits(Vector2 limits, TrialMatrixType trialMatrixType)
        //{
        //    Settings settings = m_SettingsByData[trialMatrixType];
        //    settings.Limits = limits;
        //    //foreach (var trial in trialMatrixType)
        //    //{

        //    //}
        //}
        #endregion

        #region Structs
        public struct Settings
        {
            #region Properties
            public Vector2 Limits { get; set; }
            public bool AutoLimits { get; set; }
            public Dictionary<data.ChannelStruct, Data.TrialMatrix.TrialMatrix> TrialMatrixByChannel { get; set; }
            #endregion

            #region Constructors
            public Settings(Vector2 limits, bool autoLimits, Dictionary<data.ChannelStruct, Data.TrialMatrix.TrialMatrix> trialMatrixByChannel)
            {
                Limits = limits;
                AutoLimits = autoLimits;
                TrialMatrixByChannel = trialMatrixByChannel;
            }
            #endregion
        }
        #endregion
    }
}