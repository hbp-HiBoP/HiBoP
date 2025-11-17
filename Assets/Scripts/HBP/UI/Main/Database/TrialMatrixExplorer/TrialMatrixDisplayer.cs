using Cysharp.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Enums;
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
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Database
{
    public class TrialMatrixDisplayer : MonoBehaviour
    {
        #region Properties
        [SerializeField] TrialMatrixGrid m_TrialMatrixGrid;
        [SerializeField] GameObject m_TrialMatrixGridContainer;
        [SerializeField] GameObject m_NoDataContainer;
        [SerializeField] Text m_NoDataText;
        [SerializeField] ChannelList m_ChannelList;
        [SerializeField] Dropdown m_PatientDropdown;
        [SerializeField] Button m_TrialMatrixActionsButton;
        [SerializeField] CircularDropdown m_ProtocolDropdown;
        [SerializeField] InformationPanels m_InformationPanels;
        [SerializeField] Texture2D m_Colormap;

        private List<Patient> m_Patients;
        private string m_DataName;
        private List<ChannelStruct> m_ChannelStructs;
        private List<IEEGDataInfo> m_DataInfos;
        private List<Protocol> m_AvailableProtocols;

        private ChannelStruct m_CurrentChannelStruct;
        private IEEGDataInfo m_CurrentDataInfo;
        private Patient m_CurrentPatient;
        private Protocol m_CurrentProtocol;

        public Patient CurrentPatient => m_CurrentPatient;
        
        public bool HasMultiplePatients => m_Patients != null && m_Patients.Count > 1;

        Data.Informations.TrialMatrix.TrialMatrixGrid m_TrialMatrixGridData;
        Settings m_Settings;

        public bool Visible
        {
            get
            {
                return m_TrialMatrixGrid.gameObject.activeSelf && m_ChannelList.gameObject.activeSelf && m_PatientDropdown.gameObject.activeSelf && m_ProtocolDropdown.gameObject.activeSelf;
            }
            set
            {
                m_TrialMatrixGrid.gameObject.SetActive(value);
                m_ChannelList.gameObject.SetActive(value);
                m_PatientDropdown.gameObject.SetActive(value);
                m_TrialMatrixActionsButton.gameObject.SetActive(value);
                m_ProtocolDropdown.gameObject.SetActive(value);
                m_InformationPanels.gameObject.SetActive(value);
            }
        }

        private Selector m_ParentSelector;
        #endregion

        #region Public Methods
        public void Set(List<Patient> patients, string dataName)
        {
            m_ChannelStructs = new List<ChannelStruct>();
            m_Patients = patients;
            m_DataName = dataName;
            m_DataInfos = DatabaseManager.Database.DataInfos.OfType<IEEGDataInfo>().Where(d => d.Name == m_DataName).ToList();
            LoadingManager.Load((update, token) => LoadDataAsync(update, token));
        }
        public void Set(List<ChannelStruct> channelStructs, List<IEEGDataInfo> dataInfos, string dataName)
        {
            m_ChannelStructs = channelStructs.OrderBy(c => c.Channel, new SiteNameComparer()).ToList();
            m_Patients = channelStructs.Select(cs => cs.Patient).Distinct().ToList();
            m_DataName = dataName;
            m_DataInfos = dataInfos.Where(d => d.Name == m_DataName).ToList();
            LoadingManager.Load((update, token) => LoadDataAsync(update, token));
        }
        public void Display(ChannelStruct channelStruct, IEEGDataInfo dataInfo)
        {
            m_CurrentChannelStruct = channelStruct;
            m_CurrentDataInfo = dataInfo;

            if (m_CurrentChannelStruct == null || m_CurrentDataInfo == null)
            {
                DisplayMatrix(false);
                return;
            }

            SaveSettings();
            DisplayMatrix(true);
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
            m_ChannelList.OnReachEnd.AddSafeListener(NavigateToNextPatient, gameObject);
            m_ChannelList.OnReachBeginning.AddSafeListener(NavigateToPreviousPatient, gameObject);
            m_PatientDropdown.onValueChanged.AddSafeListener(index => OnChangePatient(m_Patients[index]), gameObject);
            m_ProtocolDropdown.OnValueChanged.AddSafeListener(index => OnChangeProtocol(index), gameObject);
            m_ParentSelector = GetComponentInParent<Selector>();
        }
        private void Update()
        {
            if (m_ParentSelector != null && !m_ParentSelector.Selected)
                return;

            if (Input.GetKeyDown(KeyCode.LeftArrow))
                m_ProtocolDropdown.SelectPrevious();
            else if (Input.GetKeyDown(KeyCode.RightArrow))
                m_ProtocolDropdown.SelectNext();
        }
        private void OnChangePatient(Patient patient)
        {
            m_CurrentPatient = patient;
            UpdateChannelList();
            UpdateCurrentDataInfo();
        }
        private void OnChangeProtocol(int protocolIndex)
        {
            if (m_AvailableProtocols != null && protocolIndex >= 0 && protocolIndex < m_AvailableProtocols.Count)
            {
                m_CurrentProtocol = m_AvailableProtocols[protocolIndex];
                UpdateCurrentDataInfo();
            }
        }
        private void UpdateChannelList()
        {
            if (m_CurrentPatient == null || m_ChannelStructs == null) return;

            var channelStructsForPatient = m_ChannelStructs.Where(cs => cs.Patient == m_CurrentPatient).ToList();
            m_ChannelList.Set(channelStructsForPatient);
            m_CurrentChannelStruct = channelStructsForPatient.FirstOrDefault();
            UpdateCurrentDataInfo();
        }
        private void UpdateCurrentDataInfo()
        {
            if (m_CurrentPatient == null || m_CurrentProtocol == null || m_DataInfos == null) return;
            
            var dataInfo = m_DataInfos.FirstOrDefault(d => d.Patient == m_CurrentPatient && d.Protocol == m_CurrentProtocol);
            Display(m_CurrentChannelStruct, dataInfo);
        }
        private void SetupDropdowns()
        {
            // Setup patient dropdown
            m_PatientDropdown.options = m_Patients.Select(p => new Dropdown.OptionData(p.CompleteName)).ToList();
            if (m_Patients.Count > 0)
            {
                m_CurrentPatient = m_Patients.First();
                m_PatientDropdown.SetValueWithoutNotify(0);
            }

            // Setup protocol dropdown with all available protocols
            m_ProtocolDropdown.Options = m_AvailableProtocols.Select(p => new Dropdown.OptionData(p.Name)).ToList();
            if (m_AvailableProtocols.Count > 0)
            {
                m_CurrentProtocol = m_AvailableProtocols.First();
                m_ProtocolDropdown.SetValue(0);
            }

            // Initialize channel list and data info
            UpdateChannelList();
        }
        private async UniTask LoadDataAsync(Action<float, float, LoadingText> updateProgress, CancellationToken token)
        {
            await UniTask.SwitchToThreadPool();
            
            bool getChannelStructsFromData = m_ChannelStructs.Count == 0;
            int currentPatientIndex = 0;
            int totalPatients = m_Patients.Count;
            List<IEEGDataInfo> skippedDataInfos = new();

            try
            {
                foreach (var patient in m_Patients)
                {
                    token.ThrowIfCancellationRequested();
                    // Get all data infos for this patient with the specified data name
                    var patientDataInfos = m_DataInfos.Where(d => d.Patient == patient).ToList();
                    var patientLoadedData = new List<Core.Data.IEEGData>();

                    // Load all data for this patient
                    float patientProgress = (float)currentPatientIndex / totalPatients;
                    float dataCounter = 0;
                    foreach (var dataInfo in patientDataInfos)
                    {
                        token.ThrowIfCancellationRequested();
                        float dataProgress = dataCounter / patientDataInfos.Count;
                        updateProgress(patientProgress + (dataProgress / totalPatients), 0, new LoadingText("Loading data for ", $"{patient.CompleteName} - {dataInfo.Protocol.Name}", $" {currentPatientIndex + 1} / {totalPatients}"));
                        try
                        {
                            patientLoadedData.Add(DataManager.GetData(dataInfo) as Core.Data.IEEGData);
                        }
                        catch (HBPException e)
                        {
                            Debug.LogException(e);
                            await UniTask.SwitchToMainThread();
                            int result = await DialogBoxManager.OpenAsync(DialogBoxType.Error, "Data Loading Error", $"Failed to load data for {patient.CompleteName} - {dataInfo.Protocol.Name} - {dataInfo.Name}:\n\n{e.Title}\n{e.Message}\n\nDo you want to skip this data and continue loading, or cancel the entire operation?", "Skip data", "Cancel");
                            if (result == 0)
                            {
                                skippedDataInfos.Add(dataInfo);
                            }
                            else
                            {
                                throw new OperationCanceledException(token);
                            }
                            await UniTask.SwitchToThreadPool();
                        }
                        dataCounter++;
                    }

                    // Get channels for this patient from their loaded data
                    if (getChannelStructsFromData && patientLoadedData.Count > 0)
                    {
                        var patientChannels = patientLoadedData.SelectMany(d => d.UnitByChannel.Keys).OrderBy(c => c, new SiteNameComparer()).Distinct().Select(channel => new ChannelStruct(channel, patient)).ToList();
                        m_ChannelStructs.AddRange(patientChannels);
                    }

                    // Remove skipped data infos from the main list
                    foreach (var skippedDataInfo in skippedDataInfos)
                    {
                        m_DataInfos.Remove(skippedDataInfo);
                    }

                    currentPatientIndex++;
                }
            }
            catch (OperationCanceledException e)
            {
                await UniTask.SwitchToMainThread();
                GetComponentInParent<Window>()?.Close();
                throw e;
            }

            DataManager.NormalizeiEEGData();

            // Get all available protocols
            m_AvailableProtocols = m_DataInfos.Select(d => d.Protocol).Distinct().OrderBy(p => p.Name).ToList();

            // Set UI
            await UniTask.SwitchToMainThread();
            SetupDropdowns();
            Visible = m_ChannelStructs.Count > 0 && m_DataInfos.Count > 0;
        }
        private void DisplayMatrix(bool display)
        {
            m_TrialMatrixGridContainer.SetActive(display);
            m_NoDataContainer.SetActive(!display);
            m_NoDataText.text = display ? string.Empty : $"No data of {m_CurrentProtocol.Name} available for {m_CurrentPatient.CompleteName}.";

            if (display)
            {
                List<Data.Informations.TrialMatrix.TrialMatrixGrid.TrialMatrixData> dataToDisplay = new() { new Data.Informations.TrialMatrix.TrialMatrixGrid.IEEGTrialMatrixData(new Dataset(m_CurrentDataInfo.Protocol.Name, m_CurrentDataInfo.Protocol, new DataInfo[] { m_CurrentDataInfo }), m_CurrentDataInfo.Name, m_CurrentDataInfo.Protocol.OrderedBlocs.ToList()) };
                m_TrialMatrixGridData = new Data.Informations.TrialMatrix.TrialMatrixGrid(new ChannelStruct[] { m_CurrentChannelStruct }, dataToDisplay.ToArray());
                m_TrialMatrixGrid.Display(m_TrialMatrixGridData, $"{m_CurrentPatient.CompleteName} - {m_CurrentDataInfo.Protocol.Name} - {m_CurrentDataInfo.Name} - {m_CurrentChannelStruct.Channel}", m_Colormap);
                m_InformationPanels.Set(m_CurrentChannelStruct);
            }
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
        private void NavigateToNextPatient()
        {
            if (!HasMultiplePatients) return;
            
            int currentIndex = m_Patients.IndexOf(m_CurrentPatient);
            int nextIndex = (currentIndex + 1) % m_Patients.Count;
            
            m_PatientDropdown.SetValueWithoutNotify(nextIndex);
            OnChangePatient(m_Patients[nextIndex]);
            
            m_ChannelList.SelectFirst();
        }
        private void NavigateToPreviousPatient()
        {
            if (!HasMultiplePatients) return;
            
            int currentIndex = m_Patients.IndexOf(m_CurrentPatient);
            int previousIndex = currentIndex == 0 ? m_Patients.Count - 1 : currentIndex - 1;
            
            m_PatientDropdown.SetValueWithoutNotify(previousIndex);
            OnChangePatient(m_Patients[previousIndex]);
            
            m_ChannelList.SelectLast();
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