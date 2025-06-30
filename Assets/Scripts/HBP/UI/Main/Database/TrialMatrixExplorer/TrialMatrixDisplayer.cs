using Cysharp.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Exceptions;
using HBP.Core.Tools;
using HBP.Data.Database;
using HBP.Data.Informations;
using HBP.Data.Informations.TrialMatrix;
using HBP.Data.Preferences;
using HBP.UI.Main;
using HBP.UI.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Database
{
    public class TrialMatrixDisplayer : MonoBehaviour
    {
        #region Properties
        [SerializeField] TrialMatrixGrid m_TrialMatrixGrid;
        [SerializeField] ChannelList m_ChannelList;
        [SerializeField] CircularDropdown m_ProtocolDropdown;
        [SerializeField] Texture2D m_Colormap;

        private Patient m_Patient;
        private string m_DataName;
        private List<ChannelStruct> m_ChannelStructs;
        private List<IEEGDataInfo> m_DataInfos;
        private ChannelStruct m_CurrentChannelStruct;
        private IEEGDataInfo m_CurrentDataInfo;

        Data.Informations.TrialMatrix.TrialMatrixGrid m_TrialMatrixGridData;
        Settings m_Settings;

        public bool Visible
        {
            get
            {
                return m_TrialMatrixGrid.gameObject.activeSelf && m_ChannelList.gameObject.activeSelf && m_ProtocolDropdown.gameObject.activeSelf;
            }
            set
            {
                m_TrialMatrixGrid.gameObject.SetActive(value);
                m_ChannelList.gameObject.SetActive(value);
                m_ProtocolDropdown.gameObject.SetActive(value);
            }
        }

        private Selector m_ParentSelector;
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
            m_CurrentChannelStruct = channelStruct;
            m_CurrentDataInfo = dataInfo;

            if (m_CurrentChannelStruct == null || m_CurrentDataInfo == null)
            {
                m_TrialMatrixGrid.gameObject.SetActive(false);
                return;
            }

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
            Visible = false;
            PersistentDataManager.UserPreferences.OnSavePreferences.AddSafeListener(Refresh, gameObject);
            m_ChannelList.OnSelect.AddSafeListener(channelStruct => Display(channelStruct, m_CurrentDataInfo), gameObject);
            m_ProtocolDropdown.OnValueChanged.AddSafeListener(index => Display(m_CurrentChannelStruct, m_DataInfos[index]), gameObject);
            m_ParentSelector = GetComponentInParent<Selector>();
        }
        private void Update()
        {
            if (m_ParentSelector != null && !m_ParentSelector.Selected)
                return;

            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                m_ProtocolDropdown.SelectPrevious();
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                m_ProtocolDropdown.SelectNext();
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
                try
                {
                    updateProgress((float)progress / length, 0, new LoadingText("Loading data for ", dataInfo.Protocol.Name, $" {++progress} / {length}"));
                    loadedData.Add(DataManager.GetData(dataInfo) as Core.Data.IEEGData);
                }
                catch (HBPException e)
                {
                    Debug.LogException(e);
                    throw new CannotLoadDataInfoException(dataInfo, e.Message);
                }
            }
            DataManager.NormalizeiEEGData();

            // Create channel strucs
            m_ChannelStructs = loadedData.SelectMany(d => d.UnitByChannel.Keys).OrderBy(c => c, new SiteNameComparer()).Distinct().Select(c => new ChannelStruct(c, m_Patient)).ToList();

            // Set UI
            await UniTask.SwitchToMainThread();
            m_ChannelList.Set(m_ChannelStructs);
            m_ProtocolDropdown.Options = m_DataInfos.Select(d => new Dropdown.OptionData(d.Protocol.Name)).ToList();
            Visible = m_ChannelStructs.Count > 0 && m_DataInfos.Count > 0;
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