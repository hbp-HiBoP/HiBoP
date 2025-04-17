using Cysharp.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Tools;
using HBP.Data.Database;
using HBP.Data.Informations;
using HBP.Data.Informations.TrialMatrix;
using HBP.Data.Preferences;
using HBP.UI.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HBP.UI.Database
{
    public class TrialMatrixDisplayer : MonoBehaviour
    {
        #region Properties
        [SerializeField] TrialMatrixGrid m_TrialMatrixGrid;
        [SerializeField] Texture2D m_Colormap;

        private Patient m_Patient;
        private string m_DataName;
        private List<ChannelStruct> m_ChannelStructs;
        private List<IEEGDataInfo> m_DataInfos;
        private ChannelStruct m_CurrentChannelStruct;
        private IEEGDataInfo m_CurrentDataInfo;

        Data.Informations.TrialMatrix.TrialMatrixGrid m_TrialMatrixGridData;
        Settings m_Settings;
        #endregion

        #region Public Methods
        public void Set(Patient patient, string dataName)
        {
            m_Patient = patient;
            m_DataName = dataName;
            LoadData(patient, dataName).Forget();
        }
        public void Display(ChannelStruct channelStruct, IEEGDataInfo dataInfo)
        {
            if (channelStruct == null || dataInfo == null)
            {
                m_TrialMatrixGrid.gameObject.SetActive(false);
                return;
            }

            m_CurrentChannelStruct = channelStruct;
            m_CurrentDataInfo = dataInfo;
            List<Data.Informations.TrialMatrix.TrialMatrixGrid.TrialMatrixData> dataToDisplay = new()
            {
                new Data.Informations.TrialMatrix.TrialMatrixGrid.IEEGTrialMatrixData(new Dataset(dataInfo.Protocol.Name, dataInfo.Protocol, new DataInfo[] { dataInfo }), dataInfo.Name, dataInfo.Protocol.Blocs)
            };
            SaveSettings();
            m_TrialMatrixGridData = new Data.Informations.TrialMatrix.TrialMatrixGrid(new ChannelStruct[] { channelStruct }, dataToDisplay.ToArray());
            m_TrialMatrixGrid.gameObject.SetActive(true);
            m_TrialMatrixGrid.Display(m_TrialMatrixGridData, $"{m_Patient.CompleteName} - {dataInfo.Protocol.Name} - {dataInfo.Name} - {channelStruct.Channel}", m_Colormap);
            ApplySettings();
        }
        public void Refresh()
        {
            DataManager.NormalizeiEEGData();
            Display(m_CurrentChannelStruct, m_CurrentDataInfo);
        }
        #endregion

        #region Private Methods
        private void Awake()
        {
            m_Settings = new Settings();
            m_TrialMatrixGrid.gameObject.SetActive(false);
            PersistentDataManager.UserPreferences.OnSavePreferences.AddSafeListener(Refresh, gameObject);
        }
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                int index = (m_ChannelStructs.IndexOf(m_CurrentChannelStruct) + 1 + m_ChannelStructs.Count) % m_ChannelStructs.Count;
                Display(m_ChannelStructs[index], m_CurrentDataInfo);
            }
            else if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                int index = (m_ChannelStructs.IndexOf(m_CurrentChannelStruct) - 1 + m_ChannelStructs.Count) % m_ChannelStructs.Count;
                Display(m_ChannelStructs[index], m_CurrentDataInfo);
            }
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                int index = (m_DataInfos.IndexOf(m_CurrentDataInfo) - 1 + m_DataInfos.Count) % m_DataInfos.Count;
                Display(m_CurrentChannelStruct, m_DataInfos[index]);
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                int index = (m_DataInfos.IndexOf(m_CurrentDataInfo) + 1 + m_DataInfos.Count) % m_DataInfos.Count;
                Display(m_CurrentChannelStruct, m_DataInfos[index]);
            }
        }
        private async UniTaskVoid LoadData(Patient patient, string dataName)
        {
            await LoadingManager.LoadAsync(update => LoadDataAsync(patient, dataName, update));
        }
        private async UniTask LoadDataAsync(Patient patient, string dataName, Action<float, float, LoadingText> updateProgress)
        {
            await UniTask.SwitchToThreadPool();

            // Load data
            m_DataInfos = DatabaseManager.Database.DataInfos.OfType<IEEGDataInfo>().Where(d => d.Patient == patient && d.Name == dataName).ToList();
            int progress = 0;
            int length = m_DataInfos.Count;

            var loadedData = new List<Core.Data.IEEGData>();
            foreach (var dataInfo in m_DataInfos)
            {
                updateProgress((float)progress / length, 0, new LoadingText("Loading data for ", dataInfo.Protocol.Name, $" {++progress} / {length}"));
                loadedData.Add(DataManager.GetData(dataInfo) as Core.Data.IEEGData);
            }
            DataManager.NormalizeiEEGData();

            // Create channel strucs
            m_ChannelStructs = loadedData.SelectMany(d => d.UnitByChannel.Keys).OrderBy(c => c, new SiteNameComparer()).Distinct().Select(c => new ChannelStruct(c, m_Patient, false)).ToList();

            await UniTask.SwitchToMainThread();

            // Display first
            Display(m_ChannelStructs[0], m_DataInfos[0]);
        }
        void SaveSettings()
        {
            var data = m_TrialMatrixGrid.Data.FirstOrDefault();
            if (data != null)
            {
                m_Settings.UseDefaultLimit = data.UseDefaultLimits;
                m_Settings.Limits = data.Limits;
            }
        }
        void ApplySettings()
        {
            foreach (var data in m_TrialMatrixGrid.Data)
            {
                data.UseDefaultLimits = m_Settings.UseDefaultLimit;
                if (!m_Settings.UseDefaultLimit)
                {
                    data.Limits = m_Settings.Limits;
                }
            }
        }
        #endregion

        #region Structs
        class Settings
        {
            #region Properties
            public Vector2 Limits { get; set; }
            public bool UseDefaultLimit { get; set; }
            #endregion

            #region Constructors
            public Settings() : this(Vector2.zero, true)
            {

            }
            public Settings(Vector2 limits, bool useDefaultLimits)
            {
                Limits = limits;
                UseDefaultLimit = useDefaultLimits;
            }
            #endregion
        }
        #endregion
    }
}