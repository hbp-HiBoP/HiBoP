using Cysharp.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Tools;
using HBP.Data.Database;
using HBP.Data.BIDS;
using HBP.UI.Tools;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Main
{
    public class ExportBIDSWindow : DialogWindow
    {
        #region Properties
        [SerializeField] private InputField m_DatasetNameInputField;
        
        [SerializeField] private Button m_SelectPatientsButton;
        [SerializeField] private Text m_PatientsSelectedText;

        [SerializeField] private Toggle m_AnonymizeToggle;

        [SerializeField] private Transform m_ProtocolsContainer;
        [SerializeField] private GameObject m_ProtocolItemPrefab;
        
        [SerializeField] private Transform m_DataNamesContainer;
        [SerializeField] private GameObject m_DataNameItemPrefab;
        
        [SerializeField] private FolderSelector m_ExportFolderSelector;
        
        private List<Patient> m_AvailablePatients = new List<Patient>();
        private List<Patient> m_SelectedPatients = new List<Patient>();
        private List<BIDSProtocolItem> m_ProtocolItems = new List<BIDSProtocolItem>();
        private List<BIDSDataItem> m_DataItems = new List<BIDSDataItem>();
        #endregion
        
        #region Public Methods
        public override async void OK()
        {
            // Validation checks
            if (string.IsNullOrWhiteSpace(m_DatasetNameInputField.text))
            {
                DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, "Dataset name required", "Please enter a dataset name.").Forget();
                return;
            }
            
            if (m_SelectedPatients.Count == 0)
            {
                DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, "No patients selected", "Please select at least one patient.").Forget();
                return;
            }
            
            var selectedProtocols = m_ProtocolItems.Where(p => p.IsSelected).ToList();
            if (selectedProtocols.Count == 0)
            {
                DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, "No protocols selected", "Please select at least one protocol.").Forget();
                return;
            }
            
            var selectedData = m_DataItems.Where(d => d.IsSelected).ToList();
            if (selectedData.Count == 0)
            {
                DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, "No data selected", "Please select at least one data type.").Forget();
                return;
            }
            
            if (!Directory.Exists(m_ExportFolderSelector.Folder))
            {
                DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, "Invalid output folder", "The specified output folder does not exist.").Forget();
                return;
            }
            
            string datasetPath = Path.Combine(m_ExportFolderSelector.Folder, m_DatasetNameInputField.text);
            if (Directory.Exists(datasetPath))
            {
                int result = await DialogBoxManager.OpenAsync(Core.Enums.DialogBoxType.Warning, "Dataset exists", $"A dataset with the name '{m_DatasetNameInputField.text}' already exists in the selected folder.\n\nDo you want to overwrite it?", "Overwrite", "Cancel");
                if (result != 0)
                {
                    return;
                }
            }
            
            base.OK();
            
            await LoadingManager.LoadAsync(ExportBIDSAsync);
            DialogBoxManager.Open(Core.Enums.DialogBoxType.Informational, "Export complete", "The BIDS export is complete.").Forget();
        }
        #endregion
        
        #region Protected Methods
        protected override void Initialize()
        {
            base.Initialize();
            
            m_SelectPatientsButton.onClick.AddListener(OpenPatientSelector);
            m_DatasetNameInputField.onValueChanged.AddListener((value) => UpdateUI());
        }
        protected override void SetFields()
        {
            base.SetFields();

            // Set default dataset name
            m_DatasetNameInputField.text = "BIDS_Dataset";
            
            // Set default anonymization to off
            m_AnonymizeToggle.isOn = false;
            
            // Set default export folder
            m_ExportFolderSelector.Folder = HBP.Data.Preferences.PersistentDataManager.UserPreferences.General.Project.DefaultExportLocation;
            
            SetAvailablePatients();
            SetupProtocols();
            SetupDataNames();
            UpdateUI();
        }
        #endregion
        
        #region Private Methods
        private void SetAvailablePatients()
        {
            // Get patients that have data in the database
            m_AvailablePatients = DatabaseManager.Database.Patients
                .Where(p => DatabaseManager.Database.DataInfos.OfType<IEEGDataInfo>()
                    .Any(d => d.Patient == p))
                .OrderBy(p => p.Name)
                .ToList();
        }
        private void SetupProtocols()
        {
            // Clear existing protocol items
            foreach (var item in m_ProtocolItems)
            {
                if (item != null) Destroy(item.gameObject);
            }
            m_ProtocolItems.Clear();
            
            // Get all distinct protocol names from database data
            var protocolNames = DatabaseManager.Database.DataInfos.OfType<IEEGDataInfo>()
                .Select(d => d.Protocol.Name)
                .Distinct()
                .OrderBy(name => name)
                .ToList();
            
            foreach (var protocolName in protocolNames)
            {
                GameObject itemObj = Instantiate(m_ProtocolItemPrefab, m_ProtocolsContainer);
                BIDSProtocolItem item = itemObj.GetComponent<BIDSProtocolItem>();
                if (item != null)
                {
                    item.Initialize(protocolName);
                    item.OnToggleChanged.AddListener((value) => UpdateUI());
                    m_ProtocolItems.Add(item);
                }
            }
        }
        private void SetupDataNames()
        {
            // Clear existing data items
            foreach (var item in m_DataItems)
            {
                if (item != null) Destroy(item.gameObject);
            }
            m_DataItems.Clear();
            
            // Get all distinct data names from database
            var dataNames = DatabaseManager.Database.DataInfos.OfType<IEEGDataInfo>()
                .Select(d => d.Name)
                .Distinct()
                .OrderBy(name => name)
                .ToList();
            
            foreach (var dataName in dataNames)
            {
                GameObject itemObj = Instantiate(m_DataNameItemPrefab, m_DataNamesContainer);
                BIDSDataItem item = itemObj.GetComponent<BIDSDataItem>();
                if (item != null)
                {
                    item.Initialize(dataName);
                    item.OnToggleChanged.AddListener((value) => UpdateUI());
                    m_DataItems.Add(item);
                }
            }
        }
        private void OpenPatientSelector()
        {
            ObjectSelector<Patient> selector = WindowsManager.OpenSelector(m_AvailablePatients, this);
            selector.ObjectsSelected = m_SelectedPatients.ToArray();
            selector.OnOk.AddListener(() => OnPatientsSelected(selector.ObjectsSelected));
            WindowsReferencer.Add(selector);
        }
        private void OnPatientsSelected(Patient[] selectedPatients)
        {
            m_SelectedPatients = selectedPatients.ToList();
            UpdateUI();
        }
        private void UpdateUI()
        {
            // Update patients text
            if (m_SelectedPatients.Count == 0)
            {
                m_PatientsSelectedText.text = "No patients selected";
            }
            else if (m_SelectedPatients.Count == 1)
            {
                m_PatientsSelectedText.text = "1 patient selected";
            }
            else
            {
                m_PatientsSelectedText.text = $"{m_SelectedPatients.Count} patients selected";
            }
            
            // Enable/disable export button
            bool canExport = !string.IsNullOrWhiteSpace(m_DatasetNameInputField.text) &&
                           m_SelectedPatients.Count > 0 &&
                           m_ProtocolItems.Any(p => p.IsSelected) &&
                           m_DataItems.Any(d => d.IsSelected) &&
                           !string.IsNullOrEmpty(m_ExportFolderSelector.Folder);
            
            m_OKButton.interactable = canExport;
        }
        private async UniTask ExportBIDSAsync(System.Action<float, float, LoadingText> updateProgress, CancellationToken token)
        {
            await UniTask.SwitchToThreadPool();
            try
            {
                updateProgress?.Invoke(0, 0, new LoadingText("Initializing BIDS export"));
                
                // Create BIDS patients list based on anonymization setting
                var selectedProtocols = DatabaseManager.Database.Protocols.Where(p => m_ProtocolItems.Any(item => item.IsSelected && item.Name == p.Name)).ToList();
                var selectedDataNames = m_DataItems.Where(d => d.IsSelected).Select(d => d.DataName).ToList();
                var bidsPatients = BIDSUtility.CreateBIDSPatients(m_SelectedPatients, selectedProtocols, selectedDataNames, m_AnonymizeToggle.isOn);
                
                // Create dataset directory and general files
                string datasetPath = await BIDSUtility.CreateRootDirectoryAndFilesAsync(m_DatasetNameInputField.text, bidsPatients, m_ExportFolderSelector.Folder);

                // Create patient-specific files
                int count = 0;
                int totalPatients = bidsPatients.Count;
                foreach (var patient in bidsPatients)
                {
                    token.ThrowIfCancellationRequested();
                    updateProgress?.Invoke((float)count / totalPatients, 0f, new LoadingText($"Exporting ", $"{patient.ParticipantId}", $" ({count + 1}/{totalPatients})"));
                    await BIDSUtility.ExportPatientAsync(patient, datasetPath, new BIDSParameters()); //FIXME allow user to set parameters
                    count++;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"BIDS export failed: {ex.Message}");
                throw;
            }
        }
        #endregion
    }
}